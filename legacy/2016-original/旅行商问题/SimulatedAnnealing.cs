using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 旅行商问题
{
    /// <summary>
    /// 模拟退火算法
    /// </summary>
    class SimulatedAnnealing
    {

        //* J(y)：在状态y时的评价函数值
        int[] Y1;//* Y(i)：表示当前状态
        int[] Y2;//* Y(i+1)：表示新的状态
        public double r;//* r： 用于控制降温的快慢 . 降温退火 ，0<r<1 。r越大，降温越慢；r越小，降温越快
        public double T=0;//* T： 系统的温度，系统初始应该要处于一个高温的状态 
        public double T_min;//* T_min ：温度的下限，若温度T达到T_min，则停止搜索(暂时无用)

        int orderSize;
        Point[] points;
        private int maxPath;

        double J(int[] order) //* J(y)：在状态y时的评价函数值
        {
            double sum = 0;
            sum += distance(points[0], points[order[0]]);
            for (int i = 0; i < order.Length - 1; i++)
            {
                sum += distance(points[order[i]], points[order[i + 1]]);
            }
            sum += distance(points[order[orderSize - 1]], points[0]);
            return maxPath - sum;
        }
        /// <summary>
        /// 模拟退火算法
        /// </summary>
        /// <param name="points">点集数组</param>
        /// <param name="r">用于控制降温的快慢 . 降温退火 ，0<r<1 ，r越大，降温越慢；r越小，降温越快</param>
        /// <param name="T">系统的温度，系统初始应该要处于一个高温的状态</param>
        /// <param name="T_min">温度的下限，若温度T达到T_min，则停止搜索(暂时无用)</param>
        /// <returns>最佳序列</returns>
        public SimulatedAnnealing(Point[] points,int size, double r, double T, double T_min=0)
        {
            this.points = points;
            this.orderSize = size - 1;
            this.r = r;
            this.T = T;
            this.T_min = T_min;
            maxPath = (orderSize + 1) * 1000;
            Y1 = new int[orderSize];
            Y2 = new int[orderSize];
        }
        public double distance(Point p1, Point p2)//计算俩点间的距离
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
        public double dE;//评估值的差值，将其共有可以用来估计温度值的设定
        public int steps_total=0;
        public int[] run(int steps)
        {
            int step = steps;
            Random ran = new Random();
            if (steps_total == 0)
            {
                //初始化俩个状态
                for (int i = 0; i < orderSize; i++)
                {
                    Y1[i] = i + 1;
                    Y2[i] = i + 1;
                }
            }

            while (step>0 && T > T_min)
            {
                //产生新的序列
                int d1 = (int)(ran.NextDouble() * orderSize);//随机的逆转点
                int d2 = (int)(ran.NextDouble() * orderSize);//随机的逆转点
                int temp = Y1[d1];
                Y1[d1] = Y1[d2];  //将原来的Y1变成新的序列
                Y1[d2] = temp;
                

                dE =  J(Y1) - J(Y2);

                if (dE > 0) //表达移动后得到更优解，则总是接受移动
                    Array.Copy(Y1, Y2, orderSize); //接受从Y(i)到Y(i+1)的移动
                else
                {
                    // 函数exp( dE/T )的取值范围是(0,1) ，dE/T越大，则exp( dE/T )也
                    deT = Math.Exp(dE / T);
                    if (deT > ran.NextDouble())//0-1的随机数
                        Array.Copy(Y1, Y2, orderSize); //接受从Y(i)到Y(i+1)的移动
                }
                T = r * T; //降温退火 ，0<r<1 。r越大，降温越慢；r越小，降温越快
                           /*
                         　　* 若r过大，则搜索到全局最优解的可能会较高，但搜索的过程也就较长。若r过小，则搜索的过程会很快，但最终可能会达到一个局部最优值
                         　　*/
                           //i++;
                step--;
                steps_total++;
            }
            return Y2;
        }
        public double deT;
    }
}

