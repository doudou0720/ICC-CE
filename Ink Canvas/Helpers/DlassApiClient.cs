using Newtonsoft.Json;
using Flurl;
using Flurl.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// Dlass API 客户端，用于与服务端通信
    /// </summary>
    public class DlassApiClient : IDisposable
    {
        private const string DEFAULT_BASE_URL = "https://dlass.tech";
        private readonly string _appId;
        private readonly string _appSecret;
        private readonly string _baseUrl;
        private IFlurlClient _flurlClient;
        private string _accessToken;
        private DateTime _tokenExpiresAt;

        private string _userToken;

        /// <summary>
        /// 初始化 Dlass API 客户端
        /// </summary>
        /// <param name="appId">应用ID</param>
        /// <param name="appSecret">应用密钥</param>
        /// <param name="baseUrl">API基础URL，如果为空则使用默认URL</param>
        /// <param name="userToken">用户Token，如果提供则优先使用用户token而不是App Secret</param>
        public DlassApiClient(string appId, string appSecret, string baseUrl = null, string userToken = null)
        {
            _appId = appId ?? throw new ArgumentNullException(nameof(appId));
            _appSecret = appSecret ?? throw new ArgumentNullException(nameof(appSecret));
            _userToken = userToken;
            _baseUrl = baseUrl ?? DEFAULT_BASE_URL;

            _baseUrl = _baseUrl.TrimEnd('/');
            if (!_baseUrl.StartsWith("http://") && !_baseUrl.StartsWith("https://"))
            {
                _baseUrl = "https://" + _baseUrl;
            }

            _flurlClient = new FlurlClient(_baseUrl)
                .WithTimeout(TimeSpan.FromSeconds(30))
                .WithHeader("User-Agent", "InkCanvas/1.0");
        }

        /// <summary>
        /// 获取访问令牌（Access Token）
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_userToken))
            {
                return _userToken;
            }

            if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiresAt.AddMinutes(-5))
            {
                return _accessToken;
            }

            try
            {
                var requestData = new
                {
                    app_id = _appId,
                    app_secret = _appSecret,
                    grant_type = "client_credentials"
                };

                var response = await _flurlClient.Request("/oauth/token")
                    .PostJsonAsync(requestData);

                var responseContent = await response.GetStringAsync();

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    _accessToken = tokenResponse.AccessToken;
                    _tokenExpiresAt = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn ?? 3600);
                    return _accessToken;
                }
                else
                {
                    throw new Exception($"获取Access Token失败: {response.StatusCode}");
                }
            }
            catch (FlurlHttpException flurlEx)
            {
                if (flurlEx.StatusCode.HasValue)
                {
                    throw new Exception($"获取Access Token失败: {flurlEx.StatusCode}", flurlEx);
                }
                else if (flurlEx.InnerException is TaskCanceledException)
                {
                    throw new Exception("获取Access Token时请求超时", flurlEx.InnerException);
                }
                else
                {
                    throw new Exception($"获取Access Token时网络错误: {flurlEx.Message}", flurlEx);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取Access Token时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public async Task<T> GetAsync<T>(string endpoint, bool requireAuth = true)
        {
            try
            {
                var request = _flurlClient.Request(endpoint);

                if (requireAuth)
                {
                    var token = await GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (!string.IsNullOrEmpty(_userToken))
                        {
                            request.WithHeader("X-User-Token", token);
                        }
                        else
                        {
                            request.WithOAuthBearerToken(token);
                        }
                    }
                }

                var response = await request.GetAsync();
                var content = await response.GetStringAsync();

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        return default(T);
                    }
                    return JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
                    throw new Exception($"API请求失败: {response.StatusCode} - {content}");
                }
            }
            catch (FlurlHttpException flurlEx)
            {
                string errorContent = flurlEx.Message;
                if (flurlEx.StatusCode.HasValue)
                {
                    try
                    {
                        errorContent = await flurlEx.GetResponseStringAsync() ?? errorContent;
                    }
                    catch
                    {
                        // 如果获取响应内容失败，保持原有的错误信息
                    }
                    throw new Exception($"API请求失败: {flurlEx.StatusCode} - {errorContent}", flurlEx);
                }
                else if (flurlEx.InnerException is TaskCanceledException)
                {
                    throw new Exception($"请求超时: {endpoint}", flurlEx.InnerException);
                }
                else
                {
                    throw new Exception($"发送请求时出错: {flurlEx.Message}", flurlEx);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"发送请求时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public async Task<T> PostAsync<T>(string endpoint, object data = null, bool requireAuth = true)
        {
            try
            {
                var request = _flurlClient.Request(endpoint);

                if (requireAuth)
                {
                    var token = await GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (!string.IsNullOrEmpty(_userToken))
                        {
                            request.WithHeader("X-User-Token", token);
                        }
                        else
                        {
                            request.WithOAuthBearerToken(token);
                        }
                    }
                }

                var response = await (data != null ? 
                    request.PostJsonAsync(data) : 
                    request.PostAsync(null));
                
                var content = await response.GetStringAsync();

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        return default(T);
                    }
                    return JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
                    throw new Exception($"API请求失败: {response.StatusCode} - {content}");
                }
            }
            catch (FlurlHttpException flurlEx)
            {
                string errorContent = flurlEx.Message;
                if (flurlEx.StatusCode.HasValue)
                {
                    try
                    {
                        errorContent = await flurlEx.GetResponseStringAsync() ?? errorContent;
                    }
                    catch
                    {
                        // 如果获取响应内容失败，保持原有的错误信息
                    }
                    throw new Exception($"API请求失败: {flurlEx.StatusCode} - {errorContent}", flurlEx);
                }
                else if (flurlEx.InnerException is TaskCanceledException)
                {
                    throw new Exception($"请求超时: {endpoint}", flurlEx.InnerException);
                }
                else
                {
                    throw new Exception($"发送请求时出错: {flurlEx.Message}", flurlEx);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"发送请求时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发送PUT请求
        /// </summary>
        public async Task<T> PutAsync<T>(string endpoint, object data = null, bool requireAuth = true)
        {
            try
            {
                var request = _flurlClient.Request(endpoint);

                if (requireAuth)
                {
                    var token = await GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (!string.IsNullOrEmpty(_userToken))
                        {
                            request.WithHeader("X-User-Token", token);
                        }
                        else
                        {
                            request.WithOAuthBearerToken(token);
                        }
                    }
                }

                var response = await (data != null ? 
                    request.PutJsonAsync(data) : 
                    request.PutAsync(null));
                
                var content = await response.GetStringAsync();

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        return default(T);
                    }
                    return JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
                    throw new Exception($"API请求失败: {response.StatusCode} - {content}");
                }
            }
            catch (FlurlHttpException flurlEx)
            {
                string errorContent = flurlEx.Message;
                if (flurlEx.StatusCode.HasValue)
                {
                    try
                    {
                        errorContent = await flurlEx.GetResponseStringAsync() ?? errorContent;
                    }
                    catch
                    {
                        // 如果获取响应内容失败，保持原有的错误信息
                    }
                    throw new Exception($"API请求失败: {flurlEx.StatusCode} - {errorContent}", flurlEx);
                }
                else if (flurlEx.InnerException is TaskCanceledException)
                {
                    throw new Exception($"请求超时: {endpoint}", flurlEx.InnerException);
                }
                else
                {
                    throw new Exception($"发送请求时出错: {flurlEx.Message}", flurlEx);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"发送请求时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        public async Task<bool> DeleteAsync(string endpoint, bool requireAuth = true)
        {
            try
            {
                var request = _flurlClient.Request(endpoint);

                if (requireAuth)
                {
                    var token = await GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (!string.IsNullOrEmpty(_userToken))
                        {
                            request.WithHeader("X-User-Token", token);
                        }
                        else
                        {
                            request.WithOAuthBearerToken(token);
                        }
                    }
                }

                var response = await request.DeleteAsync();

                return response.StatusCode >= 200 && response.StatusCode < 300;
            }
            catch (FlurlHttpException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 上传笔记文件
        /// </summary>
        /// <param name="endpoint">上传端点</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="boardId">白板ID</param>
        /// <param name="secretKey">白板密钥</param>
        /// <param name="title">笔记标题（可选）</param>
        /// <param name="description">笔记描述（可选）</param>
        /// <param name="tags">笔记标签（可选）</param>
        public async Task<T> UploadNoteAsync<T>(string endpoint, string filePath, string boardId, string secretKey, string title = null, string description = null, string tags = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }

                var request = _flurlClient.Request(endpoint)
                    .WithHeader("X-Board-ID", boardId)
                    .WithHeader("X-Secret-Key", secretKey);

                using (var content = new Flurl.Http.Content.CapturedMultipartContent())
                {
                    // 添加文件
                    content.AddFile("file", filePath);

                    // 添加可选参数
                    if (!string.IsNullOrEmpty(title))
                    {
                        content.AddString("title", title);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        content.AddString("description", description);
                    }
                    if (!string.IsNullOrEmpty(tags))
                    {
                        content.AddString("tags", tags);
                    }

                    var response = await request.PostMultipartAsync(content);
                    var responseContent = await response.GetStringAsync();

                    if (response.StatusCode >= 200 && response.StatusCode < 300)
                    {
                        if (string.IsNullOrEmpty(responseContent))
                        {
                            return default(T);
                        }
                        return JsonConvert.DeserializeObject<T>(responseContent);
                    }
                    else
                    {
                        throw new Exception($"上传文件失败: {response.StatusCode} - {responseContent}");
                    }
                }
            }
            catch (FlurlHttpException flurlEx)
            {
                if (flurlEx.StatusCode.HasValue)
                {
                    throw new Exception($"上传文件失败: {flurlEx.StatusCode}", flurlEx);
                }
                else if (flurlEx.InnerException is TaskCanceledException)
                {
                    throw new Exception($"上传文件超时: {endpoint}", flurlEx.InnerException);
                }
                else
                {
                    throw new Exception($"上传文件时网络错误: {flurlEx.Message}", flurlEx);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"上传文件时出错: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _flurlClient?.Dispose();
        }

        #region 内部类

        /// <summary>
        /// Token响应模型
        /// </summary>
        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int? ExpiresIn { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }

        #endregion
    }
}

