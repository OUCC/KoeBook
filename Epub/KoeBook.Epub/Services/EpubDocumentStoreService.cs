using System.Threading.Tasks;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;

namespace KoeBook.Epub.Services;

public class EpubDocumentStoreService : IEpubDocumentStoreService
{
    private readonly List<EpubDocument> _documents = [];
    public IReadOnlyList<EpubDocument> Documents => _documents;

    public void Register(EpubDocument document, CancellationToken cancellationToken)
    {
        lock (_documents)
        {
            var id = document.Id;
            if (_documents.Any(e => e.Id == id))
                throw new ArgumentException($"The key {id} is already registered in {nameof(EpubDocumentStoreService)}");
            _documents.Add(document);
        }
        cancellationToken.Register(() => Unregister(document.Id));
    }

    public void Unregister(Guid id)
    {
        EpubDocument? document;
        lock (_documents)
        {
            document = _documents.Where(doc => doc.Id == id).FirstOrDefault();
            if (document == null)
                throw new ArgumentException($"The key {id} is already unregistered in {nameof(EpubDocumentStoreService)}");
            _documents.Remove(document);
        }
    }
}
