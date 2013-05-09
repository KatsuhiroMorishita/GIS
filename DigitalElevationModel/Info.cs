/****************************************************************************
 * Info.cs
 * DEMファイルの属性を記した構造体
 * 
 * [history]
 *      2012/5/22   メンバ変数を完全に隠ぺい化して、プロパティで情報を受け渡すように変更した。
 *      2012/5/24   DEMマップファイルから読み取れる情報ではないので、ファイル名を別のクラスへ移した。
 * **************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GIS.MapBase;
using GNSS.Field;

namespace GIS.DigitalElevationModel
{
    /// <summary>
    /// DEMファイルの属性を記した構造体
    /// <para>ファイルのさまざまな属性を提供します。</para>
    /// </summary>
    public struct Info
    {
        /*　メンバ変数　************************/
        /* プロパティ ****************************/
        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName
        {
            get;
            private set;
        }
        /// <summary>
        /// ID番号
        /// </summary>
        public int ID
        {
            get;
            private set;
        }
        /// <summary>
        /// マップサイズ（東西南北方向のメッシュ数）
        /// </summary>
        public Pos Size
        {
            get;
            private set;
        }
        /// <summary>
        /// 領域
        /// </summary>
        public RectangleField Field
        {
            get;
            private set;
        }
        /// <summary>
        /// マップのモデル形式
        /// </summary>
        public Model MapModel
        {
            get;
            private set;
        }
        /// <summary>
        /// 利用可能かどうかを返す
        /// <para>true: 利用可能です。 falseだとファイル自体を利用できないことを示します。</para>
        /// </summary>
        public Boolean Available
        {
            get
            {
                if (this.Field.AreaIsZero)
                    return false;
                else
                    return true;
            }
        }
        /* メソッド ******************************/
        /// <summary>
        /// 文字列にして返す
        /// <para>フォーマットは、「ファイル名」，「地点名」，「ID番号」，「マップサイズ」，「メッシュサイズ」，「コーナーの座標」，「マップ情報の読み込み状況」</para>
        /// </summary>
        /// <returns>文字列化したDEMinfo</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(250);
            sb.Append(",").Append(this.MapName).Append(",").Append(this.ID.ToString()).Append(",");
            sb.Append(this.Size.ToString()).Append(",").Append(",");
            sb.Append(this.Field.ToString()).Append("\n");
            return sb.ToString();
        }
        /// <summary>
        /// DEMファイルを管理するための構造体のコンストラクタ
        /// </summary>
        /// <param name="mapName">地図名<para>例：熊本</para></param>
        /// <param name="id">ID番号</param>
        /// <param name="size">マップサイズ</param>
        /// <param name="field">コーナーの座標</param>
        /// <param name="model">DEMモデル名</param>
        public Info(string mapName, int id, Pos size, RectangleField field, Model model = Model.Unknown): this()
        {
            this.MapName = mapName;
            this.ID = id;
            this.Size = size;
            this.Field = field;
            this.MapModel = model;
        }
    }
}
