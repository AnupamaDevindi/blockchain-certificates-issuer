using System.Net;
using System.Text.Json;
using BlockchainCertificatesIssuer.API.Models;
using BlockchainCertificatesIssuer.API.ViewModels;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Azure.CosmosRepository.Paging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BlockchainCertificatesIssuer.API.Functions
{
    public class TrainerFunctions
    {
        private readonly ILogger _logger;
        private readonly IRepository<Trainer> trainerRepository;

        public TrainerFunctions(ILoggerFactory loggerFactory, IRepository<Trainer> repository)
        {
            _logger = loggerFactory.CreateLogger<TrainerFunctions>();
            this.trainerRepository = repository;
        }

        [Function("CreateTrainer")]
        public async Task<HttpResponseData> CreateTrainer([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "trainer")] HttpRequestData req)
        {
            _logger.LogInformation("Create a trainer.");

            try
            {
                var trainer = await JsonSerializer.DeserializeAsync<Trainer>(req.Body);
                if (trainer == null) return req.CreateResponse(HttpStatusCode.BadRequest);

                var created = await trainerRepository.CreateAsync(trainer);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(created);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [Function("GetTrainers")]
        public async Task<HttpResponseData> GetTraiers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "trainers")] HttpRequestData req)
        {
            try
            {
                var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

                var pageNumber = queryDictionary["pageNumber"];
                var pageSize = queryDictionary["pageSize"];
                var response = req.CreateResponse(HttpStatusCode.OK);

                if (string.IsNullOrWhiteSpace(pageNumber) || !int.TryParse(pageNumber, out var page) || page <= 0)
                {
                    _logger.LogWarning("No pageNumber provided.");
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    return response; ;
                }

                if (string.IsNullOrWhiteSpace(pageSize) || !int.TryParse(pageSize, out var size) || size <= 0)
                {
                    _logger.LogWarning("No pageSize provided.");
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    return response;
                }
                IPage<Trainer> trainers =
                    await trainerRepository.PageAsync(pageNumber: page, pageSize: size);

                var resource = new PaginationResultVM<Trainer>
                {
                    Size = trainers.Size,
                    Total = await trainerRepository.CountAsync(x => x.Type == nameof(Trainer)),
                    Items = trainers.Items
                };

                if (trainers == null || !trainers.Items.Any())
                {
                    _logger.LogWarning("No data.");
                    response = req.CreateResponse(HttpStatusCode.NotFound);
                    await response.WriteAsJsonAsync(resource);
                    return response;
                }
 
                await response.WriteAsJsonAsync(resource);
                return response;
         
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [Function("DeleteTrainer")]
        public async Task<HttpResponseData> DeleteCourse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "trainer/{id}")] HttpRequestData req,
        string id)
        {
            _logger.LogInformation($"Deleting trainer with ID '{id}'.");

            try
            {

                var traier = await trainerRepository.GetAsync(id);
                if (traier == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                await trainerRepository.DeleteAsync(id);

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}
