/*************************************
 * DEM.cs
 * 国土地理院が発行する数値基盤情報5, 10メートルメッシュ（標高）データを読み込み、処理するクラスです。
 * 2012/5時点ではGMLフォーマットのみに対応しています。
 * フォーマットさえ一致していれば、メッシュサイズは問いません。
 * 
 * 　開発者：K.Morishita Kumamoto-University Japan
 * 　開発履歴：
 * 　        2011/7/2    緯度経度のパスに沿った標高を返すメソッドを追加
 * 　                    ヘッダー読み込みに、メッシュサイズの読み込みを追加
 * 　                    値の取得メソッドを、相対座標にも対応させた。
 * 　        2011/7/6    任意マップの座標調整機能を追加
 * 　        2011/7/19   今後の編集方針についてメモを残す。
 * 　                    [memo]
 * 　                    今後はメッシュサイズの異なるDEMデータを扱うことが予想される。
 * 　                    現時点では、使用可能なマップの探索で初めに見つかった地図を返すようにしているので、実はもっと細かい地図がある可能性が残る。
 *                       今後は細かい地図があればそれを利用するようにプログラムを変更する必要がある。
 *                       本クラスはその場合を想定して設計しているので対応は簡単なはずである。
 *                       
 * 　                    迷いが生じるのは、要求された座標の高度を返すメソッドにおいて、重複する地図があった場合である。
 * 　                    この場合、より詳細な値がわかる細かいメッシュの地図を選択することが望ましい。
 * 　                    しかし、本クラス初期化時のXMLファイル情報読み込み順序が思ったとおりになるとは限らない。
 * 　                    そこで、XMLファイルをIndex番号を付けながら読み込んだ後、メッシュ毎に配列に振り分けるとよい。
 * 　                    つまり、メッシュサイズ毎にXMLファイル格納配列を用意する。
 * 　                     
 * 　                    今：private DEMst[] maps;
 * 　                    次：private DEMst[] maps, maps5, maps10, maps50; と宣言する。maps5～maps50は振り分け用の配列である。
 * 　                     
 *                       要求座標の検索はmaps5から行うようにして、結果はIndex番号で返す。
 * 　                    こうすることによってシームレスで合理的なシーケンスとなる。
 * 　        2011/9/17   CheckUsefulMap()をパブリックに変更
 * 　        2011/10/2   CheckUsefulMap()をprivateに変更
 * 　                    代わりに、publicのDEMexistを整備した。これは指定座標が使えるかどうかだけを返すメソッド。
 * 　        2011/11/27  2011/7/19のコメントを今見ると、別にメッシュサイズを確認するだけでもいじゃないかとも思う。
 * 　                    現時点では、どのマップが指定された座標に合致するかを確認するメソッドが何度も呼び出されている。
 * 　                    直前に呼び出されたマップがどれか記憶しておくと少し高速になるかも。
 * 　        2011/12/2   要求された座標が何番のマップであったかを記憶するように変更し、少しだけ動作を早くしてみた
 * 　        2011/12/15  指定領域のMapBaseクラスを生成するCreateMap()を整備した。
 * 　                    このメソッドは多数の基盤地図を結合して一つのマップを生成する。
 * 　                    後は・・・地形情報クラスの改造だなぁ。。
 * 　        2012/1/2    マップ読み込み時のオーバーヘッドを減らすマルチタスク処理を追加した。
 * 　                    マップを初めて生成するときの時間が1秒ほど縮まった。
 * 　                    メッシュサイズの異なるマップへの対応も若干入れたが、まだ処理方法に悩む…。
 * 　        2012/3/24   データの存在を確認するメソッドの名前を変更した。
 * 　        2012/5/12   internal修飾子を使うのを覚えたので、DEMクラス内に宣言していた構造体を別ファイル化した。
 * 　                    コメントの見直しも実施
 * 　        2012/5/17   LocalDataをジェネリック構造体にしたのに伴い、一部のコードを変更した。
 * 　        2012/5/28   ここ3日ほどを使って、ほかのクラスへ移植可能な分は移植してこちらのメソッドを簡素化した。
 * 　                    Managerクラス, Readerクラスへの対応を進めた。
 * 　        2012/6/9    SaveDemInfo()を新設した。
 * 　        2012/7/16   XMLコメントを一部見直し
 * 　        2012/8/7    AddDirWithDialog()にて、ダイアログを開いた後にキャンセルした場合エラーが発生していたのを訂正した。
 * 　        2012/8/9    Add()とAddDir()へエラー対策を実装した。
 * 　        2012/8/10   デバッグ。。。
 * 　        2012/8/11   さらにデバッグ。
 * 　        2012/8/31   AddDir()において、発見したDEMのファイル数を返さないバグを修正した。
 * 　        2012/10/8   AddDirWithDialog()で、返り値に追加したディレクトリのパスを含めるように変更した。
 * ************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using GNSS;
using GNSS.Field;
using System.Diagnostics;           // デバッグテストのストップウォッチ用
using GIS.MapBase;
using GIS.DigitalElevationModel.Reader;

namespace GIS.DigitalElevationModel
{
    /// <summary>
    /// 数値標高モデルを扱うクラス
    /// </summary>
    public class DEM
    {
        /*　列挙体　*******************************************************************/

        /*　メンバ変数　***************************************************************/
        /// <summary>
        /// DEMマップマネージャ
        /// </summary>
        private List<Manager> manager;
        /*　プロパティ　***************************************************************/
        /// <summary>
        /// 準備OK？
        /// <para>true: 利用準備完了</para>
        /// </summary>
        public Boolean Ready
        {
            get
            {
                if (this.manager.Count != 0)
                {
                    if (this.manager[0].Count != 0) return true;
                }
                return false;
            }
        }
        /// <summary>
        /// 管理下にあるDEMファイルの数
        /// </summary>
        public int Count
        {
            get
            {
                int c = 0;
                foreach (Manager man in this.manager)
                {
                    c += man.Count;
                }
                return c;
            }
        }
        /****************************　メンバ関数（メソッド）　************************/
        /// <summary>
        /// 指定座標のデータを返すことが可能かどうかを返す
        /// </summary>
        /// <param name="pos">指定座標（lat,lon）</param>
        /// <returns>true: 使用可能</returns>
        public Boolean CheckAvailability(Blh pos)
        {
            Boolean ans = false;
            foreach (Manager man in this.manager)   // 要素がなければ実行されない
            {
                if (man.CheckAvailability(pos))
                {
                    ans = true;
                    break;
                }
            }
            return ans;
        }
        /// <summary>
        /// 保持しているDEMファイル情報をKML形式ファイルで出力する
        /// </summary>
        /// <param name="fname">ファイル名</param>
        public void SaveDemInfo(string fname)
        {
            for (int i = 0; i < this.manager.Count; i++ )
            {
                using (System.IO.StreamWriter fwriter = new System.IO.StreamWriter(fname + "_" + i.ToString("00") + ".kml", false, Encoding.UTF8))
                {
                    try
                    {
                        fwriter.Write(this.manager[i].ToStringAsKml(System.Drawing.Color.FromArgb(200, (i * 170 + 100) % 255, (i * 45) % 255, (i * 120) % 255)));            // 文字列化したデータを保存する。
                    }
                    catch
                    {
                        // エラー処理があれば記述
                    }
                }
            }
            return;
        }
        /// <summary>
        /// 指定された緯度経度の標高を返す
        /// <para>保持しているDEMモデルの内、分解能が最も細かいモデルの結果を返します。</para>
        /// <para>該当なしの場合は、float.NaNを返します。</para>
        /// </summary>
        /// <param name="pos">指定座標</param>
        /// <returns>指定された座標のマップ情報を返す</returns>
        public float GetHeight(Blh pos)
        {
            this.manager.Sort();                                                        // リストを小さい順に並び替える
            float ans = float.NaN;

            for (int i = 0; i < this.manager.Count; i++ )
            {
                ans = this.manager[i].GetHeight(pos);
                if (ans != float.NaN) break;
            }
            return ans;
        }
        /// <summary>
        /// 指定された緯度経度に沿った標高を配列で返す
        /// </summary>
        /// <param name="pos">座標の格納された配列</param>
        /// <returns>指定された座標のマップ情報を返す</returns>
        public float[] GetHeight(Blh[] pos)
        {
            float[] ans = new float[pos.Length];
            Parallel.For(0, pos.Length, i =>
            {
                ans[i] = this.GetHeight(pos[i]);
            });
            return ans;
        }
        /// <summary>
        /// 指定領域をカバーするマップを生成する
        /// <para>取得可能なマップ規格のうち、最も細かく且つ指定領域でのカバー率がthresholdOfCoverRateを超える系統を使って一つのマップを生成します。</para>
        /// </summary>
        /// <param name="field">領域</param>
        /// <param name="thresholdOfCoverRate">許容カバー率<para>この閾値を下回るカバー率であれば、マップの生成を行わずにnullを返します。</para></param>
        /// <returns>生成したマップ</returns>
        public MapDem CreateMap(RectangleField field, double thresholdOfCoverRate = 0.3) 
        {
            this.manager.Sort();                                                        // リストを小さい順に並び替える
            foreach (Manager man in this.manager)
            {
                double coverRate = man.GetCoverRate(field);
                if (coverRate >= thresholdOfCoverRate)
                {
                    return man.CreateMap(field);
                }
            }
            return null;
        }
        /// <summary>
        /// 渡されたDEM情報をマネージャへ追加する
        /// </summary>
        /// <param name="dems">追加したいDEM情報</param>
        private void Add(List<DemSet> dems)
        {
            if (dems != null)
            {
                try
                {
                    // まずは既にあるマネージャに可能であれば格納させる
                    foreach (Manager man in this.manager)
                    {
                        man.Add(dems);
                    }
                    // まだ残っていれば、新規にマネージャを追加する
                    while (dems.Count != 0)                 // 全て格納するまでループする
                    {
                        Manager man = new Manager();
                        man.Add(dems);                      // 格納されると自動的にdemsは要素が削除される
                        this.manager.Add(man);              // 新メンバー追加
                    }
                }
                catch (SystemException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return;
        }
        /// <summary>
        /// ディレクトリを指定し、含まれいているDEMファイルを追加・管理下に置く
        /// <para>ヘッダ情報のみ取得されます。</para>
        /// </summary>
        /// <param name="dirName">走査したいフォルダ名</param>
        /// <returns>追加されたDEMファイルの数</returns>
        public int AddDir(string dirName)
        {
            int ans;
            Reader.Reader reader = new Reader.Reader();
            var dem = reader.ScanDir(dirName, ReadMode.HeaderOnly);
            if (dem != null)
            {
                var cnt = dem.Count;
                this.Add(dem);
                ans = cnt;
            }
            else
                ans = 0;

            return ans;
        }
        /// <summary>
        /// ダイアログを利用してディレクトリを指定し、含まれいているDEMファイルを追加・管理下に置く
        /// <para>ヘッダ情報のみ取得されます。</para>
        /// </summary>
        /// <returns>
        /// 追加されたDEMファイルの数とディレクトリ名を一括りにしたタプル
        /// <para>格納順は「追加されたファイル数・パス」です。</para>
        /// <para>パスの中身が空である場合は、ディレクトリの選択が行われなかったことを示しています。</para>
        /// </returns>
        public Tuple<int, string> AddDirWithDialog()
        {
            Reader.Reader reader = new Reader.Reader();
            var readResult = reader.ScanDirWithDialog(ReadMode.HeaderOnly);
            var dem = readResult.Item1;
            if (dem != null)
            {
                var cnt = dem.Count;
                this.Add(dem);
                return new Tuple<int,string>(cnt, readResult.Item2);
            }
            else
                return new Tuple<int, string>(0, readResult.Item2);
        }
        #region /* コンストラクタ関係 --------------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DEM() 
        {
            this.manager = new List<Manager>();
        }
        /// <summary>
        /// デスコンストラクタ
        /// </summary>
        ~DEM() { }
        #endregion
    }
}
