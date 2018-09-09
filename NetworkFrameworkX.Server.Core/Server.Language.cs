using System.Collections.Generic;
using System.IO;
using NetworkFrameworkX.Interface;

namespace NetworkFrameworkX.Server
{
    public partial class Server<TConfig>
    {
        private void InitializeLanguage()
        {
            LoadAllLang();

            LoadLang(this.Config.Language);
        }

        private Language lang = new Language();
        private Dictionary<string, Language> langList = new Dictionary<string, Language>();

        public bool LoadLang(string Name)
        {
            if (this.langList.ContainsKey(Name)) {
                this.lang = this.langList[Name];
                this.Logger.Info(string.Format(this.lang.LoadLanguage, this.lang.Name));
                return true;
            } else {
                return false;
            }
        }

        private void LoadAllLang()
        {
            DirectoryInfo folder = new DirectoryInfo(GetFolderPath(FolderPath.Lang));

            foreach (FileInfo file in folder.GetFiles("*.json")) {
                Language lang = Language.Load(file.FullName);
                if (this.langList.ContainsKey(lang.Name)) {
                    this.langList.Remove(lang.Name);
                }

                this.langList.Add(lang.Name, lang);
            }

            if (this.langList.Count == 0) {
                Language lang = new Language();
                lang.Save(Path.Combine(GetFolderPath(FolderPath.Lang), $"{lang.Name}.json"));
                this.langList.Add(lang.Name, lang);
            }
        }
    }
}