using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Traveler.Models;

namespace Traveler
{
	public class TravelerFunction
	{
		private readonly ILogger<TravelerFunction> _logger;
		private readonly CosmosClient _cosmosClient;
		private readonly Container _container;
		private string Separator1 = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
		public TravelerFunction(ILogger<TravelerFunction> log, CosmosClient cosmosClient)
		{
			_logger = log;
			_cosmosClient = cosmosClient;
			_container = _cosmosClient.GetContainer("test-db", "test-container");
		}



		[FunctionName(nameof(GetTraveler))]
		//[OpenApiIgnore]
		[OpenApiOperation(operationId: "GetTraveler", tags: new[] { "Traveler" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The id of the Traveler")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TravelerUser), Description = "The OK response")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Result), Description = "The Bad response")]
		public async Task<IActionResult> GetTraveler(
		[HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = "Traveler/{id}")] HttpRequest req, string id)
		{

			dynamic result;
			try
			{
				_logger.LogInformation("C# HTTP trigger function processed a request.");
				result = null;
				using (FeedIterator<TravelerUser> setIterator = _container.GetItemLinqQueryable<TravelerUser>()
									 .Where(b => b.id == id)
									 .ToFeedIterator())
				{
					while (setIterator.HasMoreResults)
					{
						foreach (var item in await setIterator.ReadNextAsync())
						{
							result = item;
						}
					}
				}

				if (result == null)
				{
					return new NotFoundObjectResult($"No object found with id : {id}");
				}

			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex.Message);
				result = new Result() { Code = 2000, Description = ex.Message };
				return new BadRequestObjectResult(result);
			}
			return new OkObjectResult(result);
		}

		[FunctionName(nameof(AddTraveler))]
		[OpenApiOperation(operationId: "AddTraveler", tags: new[] { "Traveler" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiRequestBody("application/json", typeof(TravelerUnit), Description = "The Traveler name parameter")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TravelerUser), Description = "The OK response")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Result), Description = "The Bad response")]
		public async Task<IActionResult> AddTraveler(
		[HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = "Traveler")] HttpRequest req)
		{

			dynamic result;
			try
			{
				_logger.LogInformation("C# HTTP trigger function processed a request.");
				string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				TravelerUnit data = JsonConvert.DeserializeObject<TravelerUnit>(requestBody);
				if (data.Name == null || data.Country == null) throw new System.Exception("The parameter name is not defined.");
				TravelerUser user = new TravelerUser() { Name = data.Name, Country = data.Country, IsNewClient = data.IsNewClient, TotalTicketsGrossPrice = data.TotalTicketsGrossPrice, result = new Result() { Code = 1000 } };
				ItemResponse<TravelerUser> response = await _container.CreateItemAsync<TravelerUser>(user, new PartitionKey(user.Country));
				//var cost = response.RequestCharge;
				result = user;
			}
			catch (System.Exception ex)
			{
			 _logger.LogError(ex.Message);
				Result error = new Result() { Code = 2000, Description = ex.Message };
				result = error;
				return new BadRequestObjectResult(error);
			}
			return new OkObjectResult(result);
		}


		[FunctionName(nameof(RemoveTraveler))]
		//[OpenApiIgnore]
		[OpenApiOperation(operationId: "RemoveTraveler", tags: new[] { "Traveler" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The id of the Traveler")]
		[OpenApiRequestBody("application/json", typeof(TravelerUnit), Description = "The Traveler name parameter")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TravelerUser), Description = "The OK response")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Result), Description = "The Bad response")]
		public async Task<IActionResult> RemoveTraveler(
		[HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Delete), Route = "Traveler/{id}")] HttpRequest req, string id)
		{
		  
			dynamic result;
			try
			{
				_logger.LogInformation("C# HTTP trigger function processed a request.");
				List<Task> concurrentDeleteTasks = new List<Task>();
				using (FeedIterator<TravelerUser> setIterator = _container.GetItemLinqQueryable<TravelerUser>()
									 .Where(b => b.id == id)
									 .ToFeedIterator())
				{
					while (setIterator.HasMoreResults)
					{
						foreach (var item in await setIterator.ReadNextAsync())
						{
							concurrentDeleteTasks.Add(_container.DeleteItemAsync<TravelerUser>(id, new PartitionKey(item.Country)));
						}
					}
					await Task.WhenAll(concurrentDeleteTasks.Take(100));
					result = new Result() { Code = 1000, Description = $"Succesully deleted items with id : {id}" };
				}


			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex.Message);
				result = new Result() { Code = 2000, Description = ex.Message };
				return new BadRequestObjectResult(result);
			}
			return new OkObjectResult(result);
		}


		
		[FunctionName(nameof(GetTravelerSQL))]
		[OpenApiOperation(operationId: "GetTravelerSQL", tags: new[] { "Traveler" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The id of the Traveler")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TravelerUser), Description = "The OK response")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Result), Description = "The Bad response")]
		public async Task<IActionResult> GetTravelerSQL(
		[HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = "Travelersql/{id}")] HttpRequest req, string id)
		{

			dynamic result;
			result = null;
			try
			{

				var headers = req.Headers;
	  
				if (!headers.TryGetValue("Traveler", out var TravelerhHeaders))
				{
					return new BadRequestObjectResult("Traveler key  not found!");
				}



				var Traveler = TravelerhHeaders.First().ToString();
	  

				_logger.LogInformation("C# HTTP trigger function processed a request.");
				string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				TravelerUser user;


				string ssql = $@"SELECT * FROM  dbo.Dummies WHERE ID = {Convert.ToInt64(id)}";

				using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
				using (SqlCommand cmd = new SqlCommand(ssql, connection))
				{
					connection.Open();
					var reader = await cmd.ExecuteReaderAsync();
					while (reader.Read())
					{
						user = new TravelerUser()
						{
							Name = reader["Name"].ToString(),
							Country = reader["Country"].ToString(),
							IsNewClient = Convert.ToBoolean(reader["IsNewClient"]),
							TotalTicketsGrossPrice = Convert.ToDecimal(reader["TotalTicketsGrossPrice"].ToString().Replace(".", Separator1)),
							 result = new Result() { Code=1000, Description="", RequestedBy = Traveler }
						};
						result = user;
					}
					connection.Close();
				}


				if (result == null)
				{
					return new NotFoundObjectResult($@"Nothing found for the id {id}");
				}

			}

			catch (System.Exception ex)
			{
				_logger.LogError(ex.Message);
				Result error = new Result() { Code = 2000, Description = ex.Message };
				result = error;
				return new BadRequestObjectResult(error);
			}
			
			
			return new OkObjectResult(result);
		}

		[FunctionName(nameof(AddTravelerSQL))]
		[OpenApiOperation(operationId: "AddTravelerSQL", tags: new[] { "Traveler" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiRequestBody("application/json", typeof(TravelerUnit), Description = "The Traveler name parameter")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(TravelerUser), Description = "The Created (201) response")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Result), Description = "The Bad response")]
		public async Task<IActionResult> AddTravelerSQL(
		[HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = "Travelersql")] HttpRequest req)
		{

			dynamic result;
			try
			{
				_logger.LogInformation("C# HTTP trigger function processed a request.");
				string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				TravelerUnit data = JsonConvert.DeserializeObject<TravelerUnit>(requestBody);
				if (data.Name == null || data.Country == null) throw new System.Exception("The parameter name is not defined.");
				TravelerUser user = new TravelerUser() { Name = data.Name, Country = data.Country, IsNewClient = data.IsNewClient, TotalTicketsGrossPrice = data.TotalTicketsGrossPrice, result = new Result() { Code = 1000 } };


				string ssql = $@"
									INSERT INTO dbo.Dummies
									(
										Name
										,Country
										,IsNewClient
										,TotalTicketsGrossPrice
									)
									VALUES
									(
										@Name
										,@Country
										,@IsNewClient
										,@TotalTicketsGrossPrice
									)
									";
				using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
					using (SqlCommand cmd = new SqlCommand(ssql, connection))
					{
						connection.Open();
						cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = data.Name;
						cmd.Parameters.Add("@Country", SqlDbType.NVarChar).Value = data.Country;
						cmd.Parameters.Add("@IsNewClient", SqlDbType.Bit).Value = data.IsNewClient;
						cmd.Parameters.Add("@TotalTicketsGrossPrice", SqlDbType.Money).Value = data.TotalTicketsGrossPrice;
						cmd.ExecuteNonQuery();
						connection.Close();
					}
				result = user;
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex.Message);
				Result error = new Result() { Code = 2000, Description = ex.Message };
				result = error;
				return new BadRequestObjectResult(error);
			}
			return new CreatedAtActionResult(nameof(AddTravelerSQL), nameof(AddTravelerSQL),result,result);
		}

		[FunctionName(nameof(UpdateTravelerSQL))]
		[OpenApiOperation(operationId: "UpdateTravelerSQL", tags: new[] { "Traveler" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The id of the Traveler")]
		[OpenApiRequestBody("application/json", typeof(TravelerUnit), Description = "The Traveler name parameter")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TravelerUser), Description = "The OK response")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Result), Description = "The Bad response")]
		public async Task<IActionResult> UpdateTravelerSQL(
		[HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = "Travelersql/{id}")] HttpRequest req, string id)
		{

			dynamic result;
			result = null;
			try
			{
				_logger.LogInformation("C# HTTP trigger function processed a request.");
				string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				TravelerUnit data = JsonConvert.DeserializeObject<TravelerUnit>(requestBody);
				if (data.Name == null || data.Country == null) throw new System.Exception("The parameter name is not defined.");
				TravelerUser user = new TravelerUser() { Name = data.Name, Country = data.Country, IsNewClient = data.IsNewClient, TotalTicketsGrossPrice = data.TotalTicketsGrossPrice, result = new Result() { Code = 1000 } };


				string ssql = $@"UPDATE dbo.Travelers
								SET
									Name = '{data.Name}'
									, Country = '{data.Country}'
									, IsNewClient = '{Convert.ToBoolean(data.IsNewClient)}'
									, TotalTicketsGrossPrice = {data.TotalTicketsGrossPrice.ToString().Replace(Separator1, ".")}
								WHERE 1=1 AND
									ID = {Convert.ToInt64(id)}";
				using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))               
					using (SqlCommand cmd = new SqlCommand(ssql, connection))
					{
						connection.Open();
						cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = data.Name;
						cmd.Parameters.Add("@Country", SqlDbType.NVarChar).Value = data.Country;
						cmd.Parameters.Add("@IsNewClient", SqlDbType.Bit).Value = data.IsNewClient;
						cmd.Parameters.Add("@TotalTicketsGrossPrice", SqlDbType.Money).Value = data.TotalTicketsGrossPrice;
						cmd.ExecuteNonQuery();
						connection.Close();
					}
				result = user;
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex.Message);
				Result error = new Result() { Code = 2000, Description = ex.Message };
				result = error;
				return new BadRequestObjectResult(error);
			}
			return new OkObjectResult(result);
		}


	}
}

