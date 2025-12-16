
    using System.IO;
    using System.Text;
    using cfg;
    using Luban;
    using UnityEditor;
    using UnityEngine;

    public class LanguageHelp
    {
        private static string FontTextPath = "Assets/Editor/Res/3500汉字+符号+英文字符集.txt";
        private static string SavePath=Application.dataPath+"/Editor/Res/LanguageText.txt";
        
        private static string ConfigPath = "Assets/Res/Config";
        [MenuItem("Tools/生成所有多语言文本")]
        public static void GenAllLanguageText()
        {
            var lang = LoadByteBuf("tblanguage.bytes");
            var langPack= LoadByteBuf("tblanguagepack.bytes");
            var tbLang=new Tblanguage();
            tbLang._LoadData(lang);
            var tbLangPack=new TblanguagePack();
            tbLangPack._LoadData(langPack);

            var stringB = new StringBuilder();
            foreach (var item in tbLang.DataList)
            {
                stringB.AppendLine(item.CN);
            }
            foreach (var item in tbLangPack.DataList)
            {
                stringB.Append(item.CN);
            }
            LogUtlis.Info(stringB.ToString());
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            var defaultText=AssetDatabase.LoadAssetAtPath<TextAsset>(FontTextPath);
            stringB.AppendLine(defaultText.text);
            File.WriteAllText(SavePath, stringB.ToString(), Encoding.UTF8);
        }
        private static ByteBuf LoadByteBuf(string file)
        {
            return new ByteBuf(AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(ConfigPath, file)).bytes);
        }
        
    }
