using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeuProjeto.Data;          // Ajuste para o namespace do seu DbContext
using SeuProjeto.Models;        // Ajuste para o namespace do model Produto

namespace SeuProjeto.Controllers
{
    /// <summary>
    /// Trecho focado APENAS na exclusão de Produto.
    /// Integre este código no seu ProdutosController existente.
    /// </summary>
    public partial class ProdutosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(ApplicationDbContext context, ILogger<ProdutosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Produtos/Delete/{id}
        /// Exibe tela de confirmação de exclusão.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0)
            {
                return BadRequest("Id de produto inválido.");
            }

            var produto = await _context.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (produto == null)
            {
                return NotFound("Produto não encontrado.");
            }

            // Validação de regra de negócio antes da confirmação.
            if (!PodeExcluirProduto(produto, out var motivoBloqueio))
            {
                TempData["ErroExclusao"] = motivoBloqueio;
                return RedirectToAction(nameof(Index));
            }

            return View(produto);
        }

        /// <summary>
        /// POST: /Produtos/Delete/{id}
        /// Confirma e executa exclusão.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Id de produto inválido.");
            }

            var produto = await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id);

            if (produto == null)
            {
                // Evita erro caso já tenha sido removido por outro usuário/processo.
                TempData["Sucesso"] = "Produto já estava removido.";
                return RedirectToAction(nameof(Index));
            }

            if (!PodeExcluirProduto(produto, out var motivoBloqueio))
            {
                TempData["ErroExclusao"] = motivoBloqueio;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Produtos.Remove(produto);
                await _context.SaveChangesAsync();

                TempData["Sucesso"] = $"Produto '{produto.Nome}' excluído com sucesso.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // Ex.: FK impedindo exclusão (produto vinculado em pedido/item).
                _logger.LogError(dbEx, "Erro de banco ao excluir produto Id={ProdutoId}", id);
                TempData["ErroExclusao"] = "Não foi possível excluir o produto porque ele possui vínculos no banco de dados.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir produto Id={ProdutoId}", id);
                TempData["ErroExclusao"] = "Ocorreu um erro inesperado ao excluir o produto.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Regra de negócio mínima para permitir exclusão.
        /// Personalize conforme o projeto principal.
        /// </summary>
        private static bool PodeExcluirProduto(Produto produto, out string motivoBloqueio)
        {
            // Exemplo de regra: não excluir produto com estoque positivo.
            if (produto.Estoque > 0)
            {
                motivoBloqueio = "Não é permitido excluir produto com estoque maior que zero.";
                return false;
            }

            motivoBloqueio = string.Empty;
            return true;
        }
    }
}
