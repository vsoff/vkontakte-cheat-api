using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VkCheatApiLibrary
{
    public delegate int AuthCodeCallback();

    public class VkClient
    {
        private const string POST_CONTENT_TYPE = "application/x-www-form-urlencoded";
        private CookieWebClient _web;
        private bool _isAuthorized;

        public VkClient()
        {
            _isAuthorized = false;

            _web = new CookieWebClient();
            _web.Encoding = Encoding.UTF8;
            _web.Headers["user-agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2421.0 Safari/537.36 OPR/32.0.1899.0";
        }

        public void SetProxy(WebProxy proxy)
        {
            try
            {
                _web.Proxy = proxy;
                _web.DownloadString("https://m.vk.com");
            }
            catch (Exception ex)
            {
                _web.Proxy = null;
                throw new Exception($"Ошибка при получении данных по веб прокси. Exception: {ex.Message}");
            }
        }

        public void Login(string email, string password)
        {
            // Заходим на главную страницу
            HtmlDocument htmlDocument = Get("https://m.vk.com");

            // Получаем роут для авторизации со страницы
            HtmlNode node = htmlDocument.DocumentNode.SelectSingleNode("//form");
            if (node == null)
                throw new Exception("На главной странице не найдена форма для логина");
            string route = node.GetAttributeValue("action", null);

            // Логинимся на сайте
            htmlDocument = Post(route, new Dictionary<string, string>()
            {
                { "email", email },
                { "pass", password }
            });

            // Проверяем, требуют ли от нас ввести телефонный код активации
            route = htmlDocument.DocumentNode.SelectSingleNode("//form")?.GetAttributeValue("action", null);
            if (route != null && OnAuthCodeRequired != null)
                htmlDocument = Post($"https://m.vk.com{route}", new Dictionary<string, string>()
                {
                    { "code", OnAuthCodeRequired().ToString() },
                    { "remember", "1" }
                });


            // Проверяем, точно ли мы зашли
            htmlDocument = Get("https://m.vk.com");

            node = htmlDocument.DocumentNode.SelectSingleNode("//li[contains(@class,'mmi_logout')]");
            if (node == null)
            {
                _isAuthorized = false;
                throw new Exception("Не удалось залогиниться");
            }

            _isAuthorized = true;
        }

        public string GetApiHash(string method)
        {
            if (!_isAuthorized)
                throw new Exception("Не авторизован");

            // Загружаем старницу API
            HtmlDocument htmlDocument = Get($"https://vk.com/dev/{method}");

            // Вытаскиваем хэш для запросов
            string hash = htmlDocument.DocumentNode.SelectSingleNode("//button[contains(@class,'dev_req_run_btn')]")?.GetAttributeValue("onclick", null).Split('\'')[1];
            if (hash == null)
                throw new Exception("Hash not found");

            return hash;
        }

        public string ExecuteMethod(string method, string apiHash, Dictionary<string, string> data)
        {
            if (!_isAuthorized)
                throw new Exception("Не авторизован");

            Dictionary<string, string> bodyData = new Dictionary<string, string>();

            bodyData.Add("act", "a_run_method");
            bodyData.Add("al", "1");
            bodyData.Add("hash", apiHash);
            bodyData.Add("method", method);

            foreach (var d in data)
                bodyData.Add($"param_{d.Key}", d.Value);

            // Получаем ответ на запрос
            _web.Headers[HttpRequestHeader.ContentType] = POST_CONTENT_TYPE;
            string json = _web.UploadString("https://vk.com/dev", EncodePostData(bodyData));

            // Отсекаем лишнюю часть ответа
            int startIndex = json.IndexOf("{\"response\":{\"items\":[");

            if (startIndex == -1)
            {
                startIndex = json.IndexOf("{\"error\":{\"");
                if (startIndex == -1)
                    throw new Exception("Ответ сервера не соответствует ожиданию", new Exception(json));
                json = json.Substring(startIndex, json.Length - startIndex);
                throw new Exception("Сервер вернул ошибку. Подробности в InnerException", new Exception(json));
            }

            json = json.Substring(startIndex, json.Length - startIndex);

            return json;
        }

        private HtmlDocument Get(string url)
        {
            _web.Headers[HttpRequestHeader.ContentType] = null;
            string pageSource = _web.DownloadString(url);
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageSource);
            return htmlDocument;
        }

        private HtmlDocument Post(string url, Dictionary<string, string> data)
        {
            _web.Headers[HttpRequestHeader.ContentType] = POST_CONTENT_TYPE;
            string pageSource = _web.UploadString(url, EncodePostData(data));
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageSource);
            return htmlDocument;
        }

        private string EncodePostData(Dictionary<string, string> data)
        {
            return string.Join("&",
                data.Select(kvp =>
                    string.Format("{0}={1}",
                        WebUtility.UrlEncode(kvp.Key),
                        WebUtility.UrlEncode(kvp.Value))
                    )
                );
        }

        /// <summary>
        /// Событие происходящее при требовании ввести код аутентификации VK из личного/смс сообщения
        /// </summary>
        public AuthCodeCallback OnAuthCodeRequired = null;
    }
}
