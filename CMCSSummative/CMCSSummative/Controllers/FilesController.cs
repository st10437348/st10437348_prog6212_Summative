using Microsoft.AspNetCore.Mvc;
using CMCSSummative.Data;
using CMCSSummative.Services;
using Microsoft.EntityFrameworkCore;

namespace CMCSSummative.Controllers
{
    public class FilesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly FileEncryptionService _fs;
        public FilesController(AppDbContext db, FileEncryptionService fs) { _db = db; _fs = fs; }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _db.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var bytes = await _fs.DecryptAsync(doc.FilePath, doc.EncryptionIVBase64);
            return File(bytes, doc.FileType, fileDownloadName: doc.FileName);
        }
    }
}


