using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace pesada_hacienda.Core
{
    public class AzureBlobStorage
    {
        private readonly BlobContainerClient _client = new("##-<AZURE CONNECTION STRING>-##", "##-<CONTAINER NAME>-##");

        public async void Test()
        {
            await foreach (BlobItem item in _client.GetBlobsAsync())
            {
                Console.WriteLine("\t" + item.Name);
            }
        }

        public async Task<Response<BlobContentInfo>> UploadFrame(SavedFrame frame)
        {
            return await _client.UploadBlobAsync(frame.Name, frame.Data);
        }
    }
}
