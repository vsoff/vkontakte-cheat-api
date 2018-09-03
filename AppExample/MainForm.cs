using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using Newtonsoft.Json;
using VkCheatApiLibrary;
using VkCheatApiLibrary.Models;

namespace VkCheatApi
{
    public partial class MainForm : Form
    {
        VkClient vk;

        public MainForm()
        {
            InitializeComponent();

            vk = new VkClient();
            vk.OnAuthCodeRequired += () =>
            {
                AuthCodeForm acf = new AuthCodeForm();
                acf.ShowDialog();
                return acf.Code;
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            vk.Login(textBox_Email.Text, textBox_Pass.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string peerId = textBox_UserId.Text;

            if (!File.Exists("pattern.html"))
            {
                MessageBox.Show("Отсутствует файл pattern.html с тегом div.img-list", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            long _val;
            if (!long.TryParse(peerId, out _val))
            {
                MessageBox.Show("Указан не числовой идентификатор пользователя", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            // Получаем API hash
            var apiHash = vk.GetApiHash("messages.getHistoryAttachments");

            // Вытаскиваем изображения из беседы, пока они есть
            string startsFrom = "";
            List<Photo> photos = new List<Photo>();
            while (true)
            {
                // Получаем ответ
                var resp = vk.ExecuteMethod("messages.getHistoryAttachments", apiHash, new Dictionary<string, string>
                {
                    { "count", "200" },
                    { "start_from", startsFrom },
                    { "peer_id", peerId },
                    { "media_type", "photo" },
                    { "photo_sizes", "0" },
                    { "v", "5.84" }
                });

                // Преобразуем ответ в объект
                var obj = JsonConvert.DeserializeObject<HistoryAttachmentsObject>(resp);

                // Если фотографий нету, то завершаем цикл
                if (obj.Response.Items.Length == 0)
                    break;

                // Добавляем все фотографии из объекта ответа
                photos.AddRange(obj.Response.Items.Select(x => x.Attachment.Photo));

                startsFrom = obj.Response.NextFrom;

                int rndSecs = (new Random().Next(0, 1000));
                Console.WriteLine($"Страница [{startsFrom}] загружена. Кол-во фоток: {photos.Count}. Random: {rndSecs}. Дата первой: {(obj.Response.Items.Length == 0 ? "LAST" : obj.Response.Items[0].Attachment.Photo.Date.ToShortDateString())}");
                Thread.Sleep(300 + rndSecs);
            }

            if (photos.Count == 0)
            {
                MessageBox.Show($"В диалоге с пользователем id{peerId} отсутствуют фотографии", "Нет фотографий", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Формируем документ
            var html = File.ReadAllText("pattern.html");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Перерабатываем данные
            List<object> data = new List<object>();
            foreach (var p in photos)
            {
                var sizes = p.Sizes.OrderByDescending(x => x.Width);
                data.Add(new
                {
                    LargeImg = sizes.First().Url,
                    SmallImg = sizes.Last().Url,
                    Data = p.Date,
                    Owner = p.OwnerId,
                    Text = p.Text
                });
            }

            // Изменение заголовка
            var userTag = doc.DocumentNode.SelectSingleNode("//a[@id='user-id']");
            userTag.InnerHtml = "id" + peerId;
            userTag.SetAttributeValue("href", $"https://vk.com/id{peerId}");

            // Добавляем данные в документ
            var script = new HtmlNode(HtmlNodeType.Element, doc, 0);
            script.Name = "script";
            script.InnerHtml = $"data = {JsonConvert.SerializeObject(data)};";
            doc.DocumentNode.SelectSingleNode("//body").AppendChild(script);

            // Сохраняем файл
            doc.Save($"{DateTime.Now.ToString("yyyy.MM.dd")}_id{peerId}_{photos.Count}.html");
        }

        private void button_SetProxy_Click(object sender, EventArgs e)
        {
            try
            {
                vk.SetProxy(new WebProxy(textBox_ProxyIp.Text, int.Parse(textBox_ProxyPort.Text)));
                MessageBox.Show("Соединение через прокси успешно установлено!", "Прокси соединение установлено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Прокси соединение не установлено", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
    }
}
