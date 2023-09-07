using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Path = System.IO.Path;

namespace WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly CustomVisionPredictionClient _predictionClient;

    private readonly IWebHostEnvironment _env;

    private readonly CustomVisionOptions _options;

    public string TempFile { get; set; }

    public int NumberOfDucks { get; set; } = 0;

    // public string? Endpoint;

    public IFormFile Upload { get; set; }

    public IndexModel(
        ILogger<IndexModel> logger,
        CustomVisionPredictionClient client,
        IWebHostEnvironment env,
        IOptions<CustomVisionOptions> options
    )
    {
        _logger = logger;
        _predictionClient = client;
        _env = env;
        _options = options.Value;
    }

    public void OnGet()
    {

    }

    public async Task OnPostAsync()
    {
        _logger.LogInformation("ProjectID {}", _options.ProjectID);
        
        if (Upload.Length <= 0)
            return;

        var uploads = Path.Combine(_env.WebRootPath, "uploads");
        var filename = "current" + Path.GetExtension(Upload.FileName);
        var filePath = Path.Combine(uploads, filename);
        Console.WriteLine(filePath);
        TempFile = filename;
        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await Upload.CopyToAsync(fileStream);
        }

        using (var image = await Image.LoadAsync(filePath))
        {
            ImagePrediction predictionResults;

            await using (var imageData = System.IO.File.OpenRead(filePath))
            {
                predictionResults = await _predictionClient.DetectImageAsync(
                    _options.ProjectID,
                    _options.ModelName,
                    imageData
                );
            }

            var copy = image.CloneAs<Rgba32>();

            var imageWidth = image.Width;
            var imageHeight = image.Height;

            _logger.LogInformation("Image width {}", imageWidth);
            var handPath = Path.Combine(uploads, "pointing-finger.png");
            using (var handImage = await Image.LoadAsync(handPath))
            {
                // Resize hand
                var resizedHand = handImage.Clone(
                    o => o.Resize(new Size(width: imageWidth / 10, height: 0))
                );

                foreach (var prediction in predictionResults.Predictions)
                {
                    if (prediction.Probability > 0.50)
                    {
                        NumberOfDucks++;
                        
                        // The bounding box sizes are proportional - convert to absolute
                        var left = Convert.ToInt32(prediction.BoundingBox.Left * imageWidth);
                        var top = Convert.ToInt32(prediction.BoundingBox.Top * imageHeight);
                        var height = Convert.ToInt32(prediction.BoundingBox.Height * imageHeight);
                        var width = Convert.ToInt32(prediction.BoundingBox.Width * imageWidth);

                        var point = new Point(x: left, y: top);
                        point.Offset(width / 2, height / 2);
                        point.Offset(-(resizedHand.Width / 2), 0);

                        copy.Mutate(o => o.DrawImage(resizedHand, point, 1f));
                    }
                }
            }

            var mutatedPath = Path.Combine(uploads, "mutated.jpg");
            _logger.LogInformation("Mutated path {}", mutatedPath);

            await copy.SaveAsync(mutatedPath);
        }
    }
}
