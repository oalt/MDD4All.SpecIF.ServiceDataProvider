/*
 * Copyright (c) MDD4All.de, Dr. Oliver Alt
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Consul;
using MDD4All.SpecIF.DataModels.Service;
using Newtonsoft.Json;

namespace MDD4All.SpecIF.ServiceDataProvider
{
	public class SpecIfServiceDataProvider : ISpecIfServiceDescriptionProvider
	{
		private string _consulURL;

		private HttpClient _httpClient = new HttpClient();

		private List<SpecIfServiceDescription> _serviceDescriptions = new List<SpecIfServiceDescription>();

		public SpecIfServiceDataProvider(string consulURL = "http://127.0.0.1:8500")
		{
			_consulURL = consulURL;

			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			InitializeListUsingConsul();
		}


		public List<SpecIfServiceDescription> GetAvailableServices()
		{
			return _serviceDescriptions;
		}

		public void Refresh()
		{
			InitializeListUsingConsul();
		}

		private void InitializeListUsingConsul()
		{
			List<SpecIfServiceDescription> result = new List<SpecIfServiceDescription>();

			ConsulClient consulClient = new ConsulClient(/*c => c.Address = new Uri(_consulURL)*/);

			try
			{
				Dictionary<string, AgentService> services = consulClient.Agent.Services().Result.Response;

				foreach (KeyValuePair<string, AgentService> service in services)
				{
					bool isSpecIfApi = service.Value.Tags.Any(tag => tag == "SpecIF-API");

					if (isSpecIfApi)
					{
						string serviceURL = $"{service.Value.Address}:{service.Value.Port}";

						Task<SpecIfServiceDescription> task = GetServiceDescription(serviceURL);

						task.Wait();

						SpecIfServiceDescription serviceDescription = task.Result;

						if (serviceDescription != null)
						{

							serviceDescription.ServiceAddress = service.Value.Address;
                            serviceDescription.ServicePort = service.Value.Port;

							result.Add(serviceDescription);

						}
					}
				}
			}
			catch(Exception)
			{ }

			_serviceDescriptions = result;
		}

		public async Task<SpecIfServiceDescription> GetServiceDescription(string url)
		{
			SpecIfServiceDescription result = new SpecIfServiceDescription();

			try
			{
				string answer = await _httpClient.GetStringAsync(url + "/specif/v1.0/serviceDescription");

				result = JsonConvert.DeserializeObject<SpecIfServiceDescription>(answer);
			}
			catch(Exception)
			{
				result = null;
			}

			return result;
		}

	}
}
