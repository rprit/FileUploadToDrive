using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace FileUploadToDrive
{
    internal class Program
    {
        internal static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, Upload a file!");

            var path = "C:\\Users\\r_pri\\Desktop\\My File To Upload.txt";
            var jsonPath = "C:\\Users\\r_pri\\Desktop\\Credentials (2).json";
            var tokenStoragePath = "C:\\Users\\r_pri\\Desktop\\tokenStorage";
            Console.WriteLine(path);

            var tokenStorage = new FileDataStore(tokenStoragePath, true);

            UserCredential credential;

            await using(var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { DriveService.ScopeConstants.Drive },
                    "tga032024",
                    CancellationToken.None,
                    tokenStorage).Result;
            }
            
            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Drive API Snippets"
            });

            // Upload file photo.jpg on drive.
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = "My File To Upload.txt"
            };
            FilesResource.CreateMediaUpload request;
            
            // Create a new file on drive.
            using (var stream = new FileStream(path,
                       FileMode.Open))
            {
                // Create a new file, with metadata and stream.
                request = service.Files.Create(
                    fileMetadata, stream, "text/plain");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            
            // Prints the uploaded file id.
            Console.WriteLine("File ID: " + file.Id);

            Console.ReadKey();
            
        }
    }
}
