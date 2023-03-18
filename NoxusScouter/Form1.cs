using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NoxusScouter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static HttpClient handler()
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };
            var request = new HttpClient(handler);
            return request;
        }

        private List<string> players = new List<string>();

        private static class Test // to do
        {
            private static List<string> found = find();
            public static string port = found[0];
            public static string token = found[1];

            private static List<string> find()
            {
                var cmd = new ProcessStartInfo("cmd.exe");
                cmd.Arguments = "/c wmic PROCESS WHERE name='LeagueClientUx.exe' GET commandline";
                cmd.RedirectStandardOutput = true;
                cmd.UseShellExecute = false;
                cmd.CreateNoWindow = true;
                var cmd_exec = Process.Start(cmd);
                var output = cmd_exec.StandardOutput.ReadToEnd();

                cmd_exec.WaitForExit();

                string app_port = "--riotclient-app-port=([0-9]*)";
                string remoting_auth_token = "--riotclient-auth-token=([\\w-]*)";

                Regex regPort = new Regex(app_port, RegexOptions.IgnoreCase);
                Regex regToken = new Regex(remoting_auth_token, RegexOptions.IgnoreCase);

                string beforePort = regPort.Match(output).ToString();
                string beforeToken = regToken.Match(output).ToString();

                List<string> lines = new List<string>();

                lines.Add(beforePort.Substring(beforePort.IndexOf("=") + 1));

                string token = beforeToken.Substring(beforeToken.IndexOf("=") + 1);

                lines.Add(token);

                return lines;
            }
        }

        private async Task<string> LCU_Request(string url, string method = "get", string body = "")
        {
            HttpMethod httpMethod = HttpMethod.Get;
            if (method.ToLower() == "post")
            {
                httpMethod = HttpMethod.Post;
            }
            var request = handler();
            string port = Test.port;
            string token = Test.token;
            var bytes = Encoding.ASCII.GetBytes($"riot:{token}");

            try
            {

                var lcuReq = new HttpRequestMessage(httpMethod, $"https://127.0.0.1:{port}{url}");
                lcuReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(bytes));
                lcuReq.Content = body == "" ? null : new StringContent($"[\"{body}\"]", Encoding.UTF8, "application/json");
                var res = await request.SendAsync(lcuReq);
                var content = await res.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private List<fullObj> objSummoner = new List<fullObj>();

        private class fullObj
        {
            public PictureBox pictureBox;
            public Label lblnick;
            public LinkLabel lblOP;
            public LinkLabel lblU;
            public LinkLabel lblLoG;
        }

        private async void refresh()
        {
            await Task.Run(() => players.Clear());
            //string s = @"{""participants"":[{""activePlatform"":null,""cid"":""7518cbd9-5290-41f9-971b-e45328c18aee@champ-select.eu2.pvp.net"",""game_name"":""Romantic Simp"",""game_tag"":""EUNE"",""muted"":false,""name"":""Romantic Simp"",""pid"":""d363245c-4fe7-5b70-8dd2-3c76bb54209c@eu2.pvp.net"",""puuid"":""d363245c-4fe7-5b70-8dd2-3c76bb54209c"",""region"":""eu2""},{""activePlatform"":null,""cid"":""7518cbd9-5290-41f9-971b-e45328c18aee@champ-select.eu2.pvp.net"",""game_name"":""Sobocinho"",""game_tag"":""EUNE"",""muted"":false,""name"":""Sobocinho"",""pid"":""4993e391-a3c8-5721-8ba8-e8659670f5c1@eu2.pvp.net"",""puuid"":""4993e391-a3c8-5721-8ba8-e8659670f5c1"",""region"":""eu2""},{""activePlatform"":null,""cid"":""7518cbd9-5290-41f9-971b-e45328c18aee@champ-select.eu2.pvp.net"",""game_name"":""Ellahathat"",""game_tag"":""EUNE"",""muted"":false,""name"":""Ellahathat"",""pid"":""f2a773f6-5e2c-5f7b-b5fc-a9954fb54a7e@eu2.pvp.net"",""puuid"":""f2a773f6-5e2c-5f7b-b5fc-a9954fb54a7e"",""region"":""eu2""},{""activePlatform"":null,""cid"":""7518cbd9-5290-41f9-971b-e45328c18aee@champ-select.eu2.pvp.net"",""game_name"":""NinaDobrevBG"",""game_tag"":""EUNE"",""muted"":false,""name"":""NinaDobrevBG"",""pid"":""02368ca3-c28b-55d1-aac8-cd677d834dab@eu2.pvp.net"",""puuid"":""02368ca3-c28b-55d1-aac8-cd677d834dab"",""region"":""eu2""},{""activePlatform"":null,""cid"":""7518cbd9-5290-41f9-971b-e45328c18aee@champ-select.eu2.pvp.net"",""game_name"":""Mrlex"",""game_tag"":""EUNE"",""muted"":false,""name"":""Mrlex"",""pid"":""95fbd9eb-c8ba-58e6-baa5-12bcf79926cf@eu2.pvp.net"",""puuid"":""95fbd9eb-c8ba-58e6-baa5-12bcf79926cf"",""region"":""eu2""}]}";
            string s = await LCU_Request("/chat/v5/participants/champ-select");

            if (s == null)
            {
                MessageBox.Show("Connecting to LoL client failed.");
                btnRefresh.Enabled = true;
                return;
            }
            //Clipboard.SetText(s);

            JObject summoners = JObject.Parse(s);
            int index = 0;
            foreach (var summoner in summoners["participants"])
            {
                HttpClient client = new HttpClient();
                var result = await client.GetStringAsync("https://jgdiff.lol/api/v1/profile?nick=" + summoner["name"].ToString());
                JObject final = JObject.Parse(result);
                players.Add(summoner["name"].ToString());

                objSummoner[index].pictureBox.Load(final["icon"].ToString());
                objSummoner[index].lblnick.Text = summoner["name"].ToString();
                objSummoner[index].lblOP.Name = summoner["name"].ToString();
                objSummoner[index].lblU.Name = summoner["name"].ToString();
                objSummoner[index].lblLoG.Name = summoner["name"].ToString();
                index++;
            }
            btnRefresh.Enabled = true;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            comboBox.SelectedIndex = 0;
            int pos = 0;
            int margin = 5;
            for (int i=1; i<=5; i++) 
            {
                PictureBox pictureBox = new PictureBox();
                pictureBox.Location = new Point(12, 55 + pos);
                pictureBox.Size = new Size(64, 64);
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.BackColor = Color.DarkGray;
                this.Controls.Add(pictureBox);

                Label lblNick = new Label();
                lblNick.Text = "Nickname";
                lblNick.Location = new Point(83, 55 + pos);
                lblNick.Size = new Size(1, 1);
                lblNick.AutoSize = true;
                this.Controls.Add(lblNick);

                LinkLabel lblOP = new LinkLabel();
                lblOP.Text = "OP.GG";
                lblOP.Location = new Point(83, 55 + 13 + pos);
                lblOP.Size = new Size(1, 1);
                lblOP.AutoSize = true;
                lblOP.LinkClicked += lblOP_linkClicked;
                this.Controls.Add(lblOP);

                LinkLabel lblU = new LinkLabel();
                lblU.Text = "U.GG";
                lblU.Location = new Point(83 + lblOP.Size.Width, 55 + 13 + pos);
                lblU.Size = new Size(1, 1);
                lblU.AutoSize = true;
                lblU.LinkClicked += lblU_linkClicked;
                this.Controls.Add(lblU);

                LinkLabel lblLoG = new LinkLabel();
                lblLoG.Text = "Porofessor";
                lblLoG.Location = new Point(83 + lblOP.Size.Width + lblU.Size.Width, 55 + 13 + pos);
                lblLoG.Size = new Size(1, 1);
                lblLoG.AutoSize = true;
                lblLoG.LinkClicked += lblLoG_linkClicked;
                this.Controls.Add(lblLoG);


                fullObj fullobj = new fullObj();
                fullobj.pictureBox = pictureBox;
                fullobj.lblnick = lblNick;
                fullobj.lblOP = lblOP;
                fullobj.lblU = lblU;
                fullobj.lblLoG = lblLoG;
                objSummoner.Add(fullobj);


                pos = pos + 64 + margin;
            }
        }

        private void lblOP_linkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel link = sender as LinkLabel;
            Process.Start("https://www.op.gg/summoners/eune/" + link.Name);
        }
        private void lblU_linkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel link = sender as LinkLabel;
            Process.Start("https://u.gg/lol/profile/eun1/"+link.Name+"/overview");
        }
        private void lblLoG_linkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel link = sender as LinkLabel;
            Process.Start("https://www.leagueofgraphs.com/summoner/eune/" + link.Name);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            btnRefresh.Enabled = false;
            refresh();
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            int value = comboBox.SelectedIndex;

            switch (value)
            {
                case 0:
                    Process.Start("https://www.op.gg/multisearch/eune?summoners=" + string.Join(",", players));
                    break;
                case 1:
                    Process.Start("https://u.gg/multisearch?summoners=" + string.Join(",", players) + "&region=eun1");
                    break;
                case 2:
                    Process.Start("https://porofessor.gg/pregame/eune/" + string.Join(",", players));
                    break;
                default:
                    break;
            }
        }
    }
}
