using AntiCaptcha.CreateTask;
using AntiCaptcha.GetTask;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AntiCaptcha
{
    public class AntiCaptchaKey
    {
        private readonly HashSet<CreateTaskResponse> _tasks;
        private readonly object _taskLockObject;
        public string ClientKey { get; }
        [JsonIgnore]
        public bool IsReady => true;

        [JsonIgnore]
        public CreateTaskResponse[] Tasks
        {

            get
            {
                lock (_taskLockObject)
                {
                    return _tasks.ToArray();
                }
            }
        }
        [JsonIgnore]
        public int QueuedCount
        {
            get
            {
                lock (_taskLockObject)
                {
                    return _tasks.Count(t => t.TaskResponse?.Status.Equals("processing") ?? string.IsNullOrEmpty(t.ErrorCode));
                }
            }

        }
        [JsonIgnore]
        public int TotalCount
        {
            get
            {
                lock (_taskLockObject)
                {
                    return _tasks.Count;
                }
            }

        }

        public AntiCaptchaKey(string clientKey)
        {
            ClientKey = clientKey;

            _taskLockObject = new object();
            _tasks = new HashSet<CreateTaskResponse>();
        }


        public async Task<GetTaskResponse> GetSolvedCaptcha(ICreateTask createTask)
        {
            var createTaskResponse = await CreateCaptchaTask(this, createTask).ConfigureAwait(false);

            lock (_taskLockObject)
                _tasks.Add(createTaskResponse);



            if (createTaskResponse.ErrorId > 0)
            {
                throw new AntiCaptchaException($"{createTaskResponse.ErrorCode}:{createTaskResponse.ErrorDescription}");
            }


            while (createTaskResponse.TaskResponse == null || createTaskResponse.TaskResponse.Status.Equals("processing"))
            {
                await Task.Delay(10000).ConfigureAwait(false);
                createTaskResponse.TaskResponse = await GetCaptchaTask(this, createTaskResponse).ConfigureAwait(false);

                if (createTaskResponse.TaskResponse == null || createTaskResponse.TaskResponse.ErrorId <= 0) continue;

                createTaskResponse.TaskResponse.Status = "error";
                throw new AntiCaptchaException($"{createTaskResponse.TaskResponse.ErrorCode}:{createTaskResponse.TaskResponse.ErrorDescription}");
            }

            return createTaskResponse.TaskResponse;
        }

        private static async Task<CreateTaskResponse> CreateCaptchaTask(AntiCaptchaKey antiCaptchaKey, ICreateTask createTask)
        {
            var request = new CreateTaskRequest(antiCaptchaKey.ClientKey, createTask, AntiCaptchaGlobals.SoftId);
            var requestJson = JsonConvert.SerializeObject(request);
            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, AntiCaptchaEndpoints.CreateTaskUrl))
            {
                httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using (var httpResponse = await AntiCaptchaGlobals.HttpClient.SendAsync(httpRequest).ConfigureAwait(false))
                {
                    httpResponse.EnsureSuccessStatusCode();
                    var value = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var ret = JsonConvert.DeserializeObject<CreateTaskResponse>(value);
                    return ret;
                }
            }
        }
        private static async Task<GetTaskResponse> GetCaptchaTask(AntiCaptchaKey antiCaptchaKey, CreateTaskResponse task)
        {
            try
            {
                var request = new GetTaskRequest(antiCaptchaKey.ClientKey, task.TaskId);
                var requestJson = JsonConvert.SerializeObject(request);
                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, AntiCaptchaEndpoints.GetTaskUrl))
                {
                    httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                    using (var httpResponse = await AntiCaptchaGlobals.HttpClient.SendAsync(httpRequest).ConfigureAwait(false))
                    {
                        httpResponse.EnsureSuccessStatusCode();
                        var value = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var ret = JsonConvert.DeserializeObject<GetTaskResponse>(value);
                        return ret;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        protected bool Equals(AntiCaptchaKey other)
        {
            return string.Equals(ClientKey, other.ClientKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((AntiCaptchaKey)obj);
        }

        public override int GetHashCode()
        {
            return (ClientKey != null ? ClientKey.GetHashCode() : 0);
        }
    }
}
