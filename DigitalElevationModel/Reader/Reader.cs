/****************************************************************************
 * Reader.cs
 * DEMを読み出すクラス
 * 
 * [目的]
 *  本クラスの目的は、2つある。一つ目は、プログラム初期の立ち上げの段階でDEMファイルを把握する事である。
 *  ファイルの数だけでなくカバー領域や種類まで把握することで、利用クラス側に対して選択肢を提供する。
 *  二つ目は、指定されたファイルに書き込まれているDEM情報を読み込ませることである。
 *  これは必要な分だけマップのインスタンスを確保させることでメモリの消費量を削減するためである。
 *  
 * [提供機能]
 *  指定されたファイルもしくはフォルダに対して操作を実行し、ヘッダ及びDEMデータ値の取得を実施する。
 *  読み込みメソッドはこのヘッダのみの場合とDEMデータのみの場合とその両方の場合の3通りの動作を用意している。
 *  なお、読み込みの際にはモデル情報があると全モデルの一通りのチェックが必要でなくなるため高速になる。
 * 
 * [圧縮ファイルについて]
 *  本プログラムでは圧縮ファイルは取り扱いません。
 *  これは一時展開および削除によりかえって速度を落とすこととHDを汚しそうであるからです。
 * 
 * [課題]
 *      1) 
 * 
 * [history]
 *      2012/5/22   開発開始
 *      2012/5/24   基本機能の全実装を終えた。
 *      2012/10/8   ScanDirWithDialog()で、返り値に追加したディレクトリのパスを含めるように変更した。
 * **************************************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using GIS.MapBase;

namespace GIS.DigitalElevationModel.Reader
{
    /// <summary>
    /// DEMファイルを読み込むクラス
    /// <para>指定したファイルやフォルダを走査して、モデル情報を返します。</para>
    /// <para>特にオブジェクトを作る必要もなさそうなのでスタティッククラスとしています。</para>
    /// </summary>
    public class Reader
    {
        /* プロパティ **********************************************************/
        /* メソッド **********************************************************/
        /// <summary>
        /// ファイルを読み込み、マップ情報を返す
        /// </summary>
        /// <param name="fileNames">ファイル名</param>
        /// <returns>マップ情報<para>圧縮ファイルの場合、一つのファイルに多数のマップが格納されている場合があるのでListで返します。</para></returns>
        public List<DemSet> ReadHeader(string[] fileNames)
        {
            List<DemSet> mapInfolist = new List<DemSet>();

            //System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            // ヘッダーの読み出し
            Info[] info = new Info[fileNames.Length];
            if (fileNames.Length == 1)                              // ファイルが単一であれば、Parallel処理はかえって重いので分ける
            {
                
                JpgisGmlReader reader = new JpgisGmlReader();
                info[0] = reader.GetHeader(fileNames[0]);
            }
            else
            {
                Parallel.For(0, fileNames.Length, i =>
                //for(int i = 0; i < fileNames.Length; i++)         // 66 ファイルあると、forよりも並列読み込みの方が若干早い。＠2012/5/28
                {
                    //System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();
                    JpgisGmlReader reader = new JpgisGmlReader();
                    info[i] = reader.GetHeader(fileNames[i]);
                    //sw2.Stop();                                                     // ストップウォッチを止める
                    //Console.WriteLine("個々のヘッダー読み込み時間: " + sw2.Elapsed); // 結果を表示する
                });
            }
            //sw.Stop();                                                              // ストップウォッチを止める
            //Console.WriteLine("合計のヘッダー読み込み時間: " + sw.Elapsed);          // 結果を表示する
            for (int i = 0; i < fileNames.Length; i++)
            {
                if (info[i].Available == true)
                    mapInfolist.Add(new DemSet(fileNames[i], info[i]));
            }
            return mapInfolist;
        }
        /// <summary>
        /// 引数のマップ情報を利用してマップを読み込む
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="info">ファイルに含まれているDEMの情報</param>
        /// <returns>マップのインスタンス</returns>
        public MapDem ReadMap(string fileName, Info info)
        {
            // マップ情報を読み出す
            float[,] height = null;
            switch (info.MapModel)
            { 
                case Model.JPGIS2x_GML:
                    JpgisGmlReader reader = new JpgisGmlReader();
                    height = reader.GetValues(fileName, info.Size);
                    break;
            }
            MapDem map = null;
            if (height != null)
            {
                map = new MapDem(info.Field);
                map.SetMapData(height);
            }
            return map;
        }
        /// <summary>
        /// 指定されたフォルダを走査して、含まれるファイル情報を取得する
        /// </summary>
        /// <param name="dirName">フォルダ名</param>
        /// <param name="mode">読み込みモード</param>
        /// <returns>DEMファイル情報</returns>
        public List<DemSet> ScanDir(string dirName, ReadMode mode = ReadMode.HeaderOnly)
        {
            List<DemSet> maplist = new List<DemSet>();
            if (System.IO.Directory.Exists(dirName) == false) return maplist;
            string[] xmlFiles = System.IO.Directory.GetFiles(dirName, "*.xml", System.IO.SearchOption.TopDirectoryOnly); // 拡張子がxmlのファイルを全て取得する
            // 並列処理でデータを取得
            maplist = this.ReadHeader(xmlFiles);

            // データ本体も取得が必要ならばこちらも実行する
            if (mode == ReadMode.HeaderAndValue)
            {
                Parallel.For(0, maplist.Count, i =>                             // すべてのファイルについて処理する（可能ならマルチスレッドで）
                {
                    maplist[i].Load();
                });
            }
            return maplist;
        }
        /// <summary>
        /// フォルダの指定にダイアログを用いて、DEMを読み込む
        /// </summary>
        /// <param name="mode">読み込みモード</param>
        /// <param name="rootFolder">探索開始とするルートディレクトリ</param>
        /// <returns>List形式のDEM情報及び選択したディレクトリへのパスをひとくくりにしたタプル<para>格納順は「DEM情報・パス」です。</para></returns>
        public Tuple<List<DemSet>, string> ScanDirWithDialog(ReadMode mode = ReadMode.HeaderOnly, Environment.SpecialFolder rootFolder = Environment.SpecialFolder.Desktop)
        {
            List<DemSet> maplist = null;
            FolderBrowserDialog fbd = new FolderBrowserDialog();                // FolderBrowserDialogクラスのインスタンスを作成
            string dirPath = "";

            // DEM格納フォルダの指定
            fbd.Description = "DEMファイルを格納したフォルダを指定して下さい。";// 上部に表示する説明テキストを指定する
            fbd.RootFolder = rootFolder;                                        // ルートフォルダを指定する
            fbd.ShowNewFolderButton = false;                                    // ユーザーが新しいフォルダを作成できないようにする
            try
            {
                if (fbd.ShowDialog() == DialogResult.OK)                        // ダイアログを表示する
                {
                    //System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();

                    maplist = this.ScanDir(fbd.SelectedPath, mode);
                    dirPath = fbd.SelectedPath;
                    //sw2.Stop();                                                             // ストップウォッチを止める
                    //Console.WriteLine("ヘッダ読み込みにかけた処理時間: " + sw2.Elapsed);   // 結果を表示する
                }
            }
            catch
            {
                
            }
            return new Tuple<List<DemSet>, string>(maplist, dirPath);
        }
        /// <summary>
        /// 静的コンストラクタ
        /// </summary>
        public Reader()
        { 
            
        }
    }
}
