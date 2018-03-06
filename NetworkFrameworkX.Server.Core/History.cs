using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.Server
{
    internal class History : IEnumerable<string>
    {
        public int MaxLength { get; set; } = 512;

        public string Path { get; set; }

        private Queue<string> HistoryList { get; } = new Queue<string>();

        public int Count => this.HistoryList.Count;

        public void Add(string item)
        {
            this.HistoryList.Enqueue(item);

            this.HistoryList.DequeueTo(this.MaxLength);

            if (!this.Path.IsNullOrEmpty()) {
                File.AppendAllText(this.Path, item + Utility.StringNewLine);
            }
        }

        public string[] ToArray() => this.HistoryList.ToArray();

        public IEnumerator<string> GetEnumerator() => this.HistoryList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.HistoryList.GetEnumerator();

        public void Load()
        {
            if (!this.Path.IsNullOrEmpty()) {
                if (File.Exists(this.Path)) {
                    string[] list = File.ReadAllLines(this.Path);
                    this.HistoryList.Clear();
                    for (int i = 0; i < list.Length; i++) {
                        this.HistoryList.Enqueue(list[i]);
                    }

                    this.HistoryList.DequeueTo(this.MaxLength);
                }
            }
        }

        public static History Load(string path)
        {
            History history = new History() { Path = path };
            history.Load();

            return history;
        }

        public bool Save(string path)
        {
            if (!path.IsNullOrEmpty()) {
                File.WriteAllLines(this.Path, this.HistoryList);
            }
            return true;
        }
    }
}