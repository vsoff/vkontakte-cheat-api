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
            string html = _web.DownloadString("https://m.vk.com");
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            // Получаем роут для авторизации со страницы
            HtmlNode node = htmlDocument.DocumentNode.SelectSingleNode("//form");
            if (node == null)
                throw new Exception("На главной странице не найдена форма для логина");
            string route = node.GetAttributeValue("action", null);

            // Логинимся на сайте
            _web.Headers[HttpRequestHeader.ContentType] = POST_CONTENT_TYPE;
            html = _web.UploadString(route, $"email={email}&pass={password}");
            htmlDocument.LoadHtml(html);

            // Проверяем, требуют ли от нас ввести телефонный код активации
            route = htmlDocument.DocumentNode.SelectSingleNode("//form")?.GetAttributeValue("action", null);
            if (route != null && OnAuthCodeRequired != null)
                html = _web.UploadString($"https://m.vk.com{route}", $"code={OnAuthCodeRequired()}&remember=1");

            // Проверяем, точно ли мы зашли
            _web.Headers[HttpRequestHeader.ContentType] = null;
            html = _web.DownloadString("https://m.vk.com");
            htmlDocument.LoadHtml(html);
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
            // Загружаем старницу API
            _web.Headers[HttpRequestHeader.ContentType] = null;
            var resultResponse = _web.DownloadString($"https://vk.com/dev/{method}");

            // Вытаскиваем хэш для запросов
            var doc = new HtmlDocument();
            doc.LoadHtml(resultResponse);

            string hash = doc.DocumentNode.SelectSingleNode("//button[contains(@class,'dev_req_run_btn')]")?.GetAttributeValue("onclick", null).Split('\'')[1];
            if (hash == null)
                throw new Exception("Hash not found");

            return hash;
        }

        public string ExecuteMethod(string method, string apiHash, Dictionary<string, string> data)
        {
            Dictionary<string, string> bodyData = new Dictionary<string, string>();

            bodyData.Add("act", "a_run_method");
            bodyData.Add("al", "1");
            bodyData.Add("hash", apiHash);
            bodyData.Add("method", method);

            foreach (var d in data)
                bodyData.Add($"param_{d.Key}", d.Value);

            // Формируем POST data
            string bodyValues = string.Join("&",
                bodyData.Select(kvp =>
                    string.Format("{0}={1}",
                        WebUtility.UrlEncode(kvp.Key),
                        WebUtility.UrlEncode(kvp.Value))
                    )
                );

            // Получаем ответ на запрос
            _web.Headers[HttpRequestHeader.ContentType] = POST_CONTENT_TYPE;
            string json = _web.UploadString("https://vk.com/dev", bodyValues);

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

        /// <summary>
        /// Событие происходящее при требовании ввести код аутентификации VK из личного/смс сообщения
        /// </summary>
        public AuthCodeCallback OnAuthCodeRequired = null;
    }
}
