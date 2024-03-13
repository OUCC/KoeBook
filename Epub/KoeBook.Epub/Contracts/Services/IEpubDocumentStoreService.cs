using KoeBook.Epub.Models;

namespace KoeBook.Epub.Contracts.Services;

public interface IEpubDocumentStoreService
{
    IReadOnlyList<EpubDocument> Documents { get; }

    void Register(EpubDocument document, CancellationToken cancellationToken);

    void Unregister(Guid id);
}
