/******************************************************
 * PolygonEllipse.cs
 * KMLのポリゴンで楕円を描画するクラス
 * 
 * [更新履歴]
 *      2012/8or9   頃、開発開始
 *      2012/10/8   コメント類を見直した。現時点では機能しない。
 * ****************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GNSS;

namespace GIS.KML.Application
{
    /// <summary>
    /// KMLのポリゴンで楕円を描画するクラス
    /// </summary>
    public class PolygonEllipse
    {
        /* メンバ変数 **************************************************/
        /// <summary>
        /// 焦点座標
        /// </summary>
        private Blh[] focus = new Blh[2];
        /// <summary>
        /// 近似ポイント数
        /// </summary>
        private int approximation;
        /// <summary>
        /// 長半径[m]
        /// </summary>
        private double a;
        /// <summary>
        /// 短半径[m]
        /// </summary>
        private double b;
        /* プロパティ **************************************************/
        /// <summary>
        /// 近似ポイント数
        /// <para>最低、2ポイントです。これ以下には設定できません。</para>
        /// </summary>
        public int ApproximationPoint
        {
            get { return this.approximation; }
            set
            {
                if (value < 2)
                    this.approximation = 2;
                else
                    this.approximation = value;
            }
        }
        /// <summary>
        /// 長半径[m]
        /// </summary>
        public double LongRadius
        {
            get { return this.a; }
            set
            {
                this.a = value;
            }
        }
        /// <summary>
        /// 短半径[m]
        /// </summary>
        public double ShortRadius
        {
            get { return this.b; }
            set
            {
                this.b = value;
            }
        }
        /// <summary>
        /// 扁平率
        /// <para>セットの際は、長半径に合わせて短半径を調整します。</para>
        /// </summary>
        public double f
        {
            get
            {
                return (this.a - this.b) / this.a;
            }
            set
            {
                this.b = this.a * (1.0 - this.f);
            }
        }
        /// <summary>
        /// エラー状況
        /// <para>true: エラーがあります。このままではKML出力されません。</para>
        /// </summary>
        public Boolean Error
        {
            get
            {
                if (this.focus[0] == this.focus[1])
                    return true;
                else
                    return false;
            }
        }
        /* メソッド **************************************************/
        /// <summary>
        /// 2点の座標と、長半径と扁平率をセットする必要がある
        /// </summary>
        public PolygonEllipse(Blh p1, Blh p2, double a, double f)
        {
            this.focus[0] = p1;
            this.focus[1] = p2;
            this.LongRadius = a;
            this.f = f;
        }
    }
}
