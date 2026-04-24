using EcommerceMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace EcommerceMVC.Controllers
{
    public partial class ProdutoController : Controller
    {
        private const string ConnectionString = "Data Source=database/database.db;";

        [HttpGet]
        public ActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Id de produto inválido.");
            }

            try
            {
                SQLitePCL.Batteries_V2.Init();

                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT id, nome, descricao, preco, estoque, imagem
                    FROM Produto
                    WHERE id = $id";
                command.Parameters.AddWithValue("$id", id);

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    return NotFound("Produto não encontrado.");
                }

                var produto = MapProduto(reader);

                if (!PodeExcluirProduto(produto, out var motivoBloqueio))
                {
                    TempData["ErroExclusao"] = motivoBloqueio;
                    return RedirectToAction(nameof(Index));
                }

                return View(produto);
            }
            catch (Exception ex)
            {
                TempData["ErroExclusao"] = $"Erro ao carregar produto para exclusão: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Id de produto inválido.");
            }

            try
            {
                SQLitePCL.Batteries_V2.Init();

                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();
                using var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = @"
                    SELECT id, nome, descricao, preco, estoque, imagem
                    FROM Produto
                    WHERE id = $id";
                selectCommand.Parameters.AddWithValue("$id", id);

                using var reader = selectCommand.ExecuteReader();
                if (!reader.Read())
                {
                    TempData["Sucesso"] = "Produto já estava removido.";
                    return RedirectToAction(nameof(Index));
                }

                var produto = MapProduto(reader);

                if (!PodeExcluirProduto(produto, out var motivoBloqueio))
                {
                    TempData["ErroExclusao"] = motivoBloqueio;
                    return RedirectToAction(nameof(Index));
                }

                using var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM Produto WHERE id = $id";
                deleteCommand.Parameters.AddWithValue("$id", id);

                var rows = deleteCommand.ExecuteNonQuery();
                if (rows == 0)
                {
                    TempData["Sucesso"] = "Produto já estava removido.";
                }
                else
                {
                    TempData["Sucesso"] = $"Produto '{produto.Nome}' excluído com sucesso.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode is 19 or 787)
            {
                TempData["ErroExclusao"] = "Não foi possível excluir o produto porque ele possui vínculos (ex.: itens de pedido).";
                return RedirectToAction(nameof(Index));
            }
            catch (SqliteException)
            {
                TempData["ErroExclusao"] = "Erro do banco SQLite ao excluir o produto.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErroExclusao"] = $"Ocorreu um erro inesperado ao excluir o produto: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private static bool PodeExcluirProduto(Produto produto, out string motivoBloqueio)
        {
            if (produto.Estoque > 0)
            {
                motivoBloqueio = "Não é permitido excluir produto com estoque maior que zero.";
                return false;
            }

            motivoBloqueio = string.Empty;
            return true;
        }

        private static Produto MapProduto(SqliteDataReader reader)
        {
            return new Produto
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Descricao = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Preco = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3),
                Estoque = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                Imagem = reader.IsDBNull(5) ? "sem-foto.jpg" : reader.GetString(5)
            };
        }
    }
}
