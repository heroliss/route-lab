using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 旅行商问题
{
    /// <summary>
    /// 遗传算法
    /// </summary>
    class GeneticAlgorithm
    {
        public double Pc;//交叉发生的概率(Probability of crossover)
        public double Pm;//变异发生的概率(Probability of mutation )
        readonly public int popSize;//种群规模(Population size)
        readonly Point[] points;//顶点坐标集
        readonly int orderSize;//序列大小,顶点坐标集大小为该值加1
        //int Tf;//进化产生的任何一个个体的适应度函数超过Tf，则可以终止进化过程(这里是路径和最小值)

        bool[] blist;//该bool数组用来找出一组不重复的随机数
        public int steps = 0;//累计进化的代数


        double maxPath;//最长路径，用来给出适应度值

        /// <summary>
        /// 适应度函数 ( Fitness Function )
        /// </summary>
        /// <param name="n">个体序号</param>
        /// <returns></returns>
        double f(int n)
        {
            double sum = 0;
            sum += distance(points[0], points[pop[n, 0]]);
            for (int i = 0; i < orderSize - 1; i++)
            {
                sum += distance(points[pop[n, i]], points[pop[n, i + 1]]);
            }
            sum += distance(points[pop[n, orderSize-1]],points[0]);
            return maxPath - sum;
        }
        public double distance(Point p1, Point p2)//计算俩点间的距离
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
        int[,] pop;
        double[] adaptation;//适应度
        public bool cancel = false;
        public GeneticAlgorithm(Point[] points, int orderSize, int popSize, double pc, double pm,bool ElitismOn,int NumOfAggression)
        {
            this.points = points;
            this.orderSize = orderSize;
            this.popSize = popSize;
            this.Pc = pc;
            this.Pm = pm;
            this.ElitismOn = ElitismOn;
            this.NumOfAggression = NumOfAggression;
            blist = new bool[orderSize];
            adaptation = new double[popSize];
            maxPath = (orderSize + 1) * 1000;
            steps = 0;
        }
        Random ran = new Random();
        public bool ElitismOn;//精英主义算法开关
        public int step;
        public int NumOfAggression;//侵略强度
        public int[] evolve(int stepTimes, out int[] order_out)//开始进化(参数：每次的进化代数)
        {
            step = stepTimes;
            //初始化Pm，Pc，M，G，Tf等参数。随机产生第一代种群Pop
            if (steps == 0)
            {
                pop = new int[popSize, orderSize]; //产生popsize大小的种群，每个个体为ordersize大小的int数组

                int i = 1, j = 0;
                while (i < popSize)
                {
                    j = 0;
                    Array.Clear(blist, 0, orderSize);
                    while (j < orderSize)
                    {
                        int n = (ran.Next() % orderSize);
                        if (blist[n] == false)
                        {
                            blist[n] = true;
                            pop[i, j] = n + 1;
                            j++;
                        }
                    }
                    i++;
                }
            }

            //do
            while (step > 0 && !cancel)
            {
                double sum = 0;
                //adaptation[0] = f(0);//单独计算0号个体的适应度
                for (int n = 1; n < popSize; n++)//计算种群Pop中每一个体的适应度F(i) (不包含0号个体)
                {
                    adaptation[n] = f(n);
                    sum += adaptation[n];
                }

                int[,] newPop = new int[popSize, orderSize];//初始化空种群newPop
                int newN1 = 1, newN2 = 2;
                do
                {
                    //根据适应度以比例选择算法从种群Pop中选出2个个体

                    int n1 = RWS(sum);
                    int n2 = RWS(sum);
                    /*---------------------------------------*/
                    if (ran.NextDouble() < Pc)//对2个个体按交叉概率Pc执行交叉操作
                    {
                        int d = (int)(ran.NextDouble() * orderSize);//随机的切分点
                        for (int j = 0; j <= d; j++)//交换基因
                        {
                            newPop[newN1, j] = pop[n1, j];
                            newPop[newN2, j] = pop[n2, j];
                        }
                        AddRemaining(newPop, newN1, n2, d);//将n2中newN1中没有的基因添加到newN1后面
                        AddRemaining(newPop, newN2, n1, d);

                    }
                    else
                    {
                        for (int j = 0; j < orderSize; j++)
                        {
                            newPop[newN1, j] = pop[n1, j];
                            newPop[newN2, j] = pop[n2, j];
                        }
                    }
                    /*-------------------------------------------------------*/
                    if (ran.NextDouble() < Pm)//对2个个体按变异概率Pm执行变异操作 
                    {
                        int d1 = (int)(ran.NextDouble() * orderSize);//随机的逆转点
                        int d2 = (int)(ran.NextDouble() * orderSize);//随机的逆转点
                        int temp = newPop[newN1, d1];
                        newPop[newN1, d1] = newPop[newN1, d2];  //突变了一个个体
                        newPop[newN1, d2] = temp;
                    }
                    //将2个新个体加入种群newPop中
                    newN1 += 2;
                    newN2 += 2;
                } while (newN2 <= popSize);

                /*-------------保存最佳个体到第一个位置-----------------*/
                //if (ElitismOn)
                //{
                double m = 0;
                int ii = 0;
                int zeroORone = 1;
                if (ElitismOn)
                    zeroORone = 0;
                for (int i = zeroORone; i < adaptation.Length; i++)  //若精英主义开启则包括0号个体
                {
                    if (m < adaptation[i])
                    {
                        m = adaptation[i];
                        ii = i;
                    }
                }
                for (int j = 0; j < orderSize; j++)
                {
                    newPop[0, j] = pop[ii, j]; //将最佳个体覆盖到新种群的0位置     

                }
                adaptation[0] = adaptation[ii];//覆盖最佳适应度  

                if (ElitismOn)//最佳个体侵略操作
                {
                    for (int i = 1; i <= NumOfAggression ; i++)
                    {
                        for (int j = 0; j < orderSize; j++)
                        {
                            newPop[i, j] = pop[ii, j];
                        }
                    }
                }
                //}
                //else
                //{
                //    adaptation[0] = 0;
                //}
                /*---------------------------------------*/
                pop = newPop;//用newPop取代Pop
                step--;
                steps++;
                order_out = new int[orderSize];
                for (int i = 0; i < orderSize; i++)
                {
                    order_out[i] = pop[0, i];
                }
            }// while (step > 0);//任何染色体得分超过Tf， 或繁殖代数超过G (此处是固定次数)

            //for (int n = 0; n < popSize; n++)//计算种群Pop中每一个体的适应度F(n)
            //{
            //    adaptation[n] = f(n);
            //}
            //double max = 0;
            //int index=0;
            //for (int i = 0; i < adaptation.Length; i++)
            //{
            //    if (max<adaptation[i])
            //    {
            //        max = adaptation[i];
            //        index = i;
            //    }
            //}
            int[] result = new int[orderSize];
            for (int i = 0; i < orderSize; i++)
            {
                result[i] = pop[0, i];
            }
            order_out = result;
            return result;
        }

        private void AddRemaining(int[,] newPop, int newN1, int n2, int d)
        {
            int k = d + 1;
            for (int j = 0; j < orderSize; j++)
            {
                int jd;
                for (jd = 0; jd <= d; jd++)
                {
                    if (pop[n2, j] == newPop[newN1, jd])
                        break;
                }
                if (jd > d)
                {
                    newPop[newN1, k] = pop[n2, j];
                    k++;
                }
            }
        }

        int RWS(double sum)
        {
            double dice = ran.NextDouble() * sum;//骰子
            int zeroORone = 1;
            //if (ElitismOn)
            //    zeroORone = 0;
            for (int n = zeroORone; n < popSize; n++)
            {
                dice -= adaptation[n];
                if (dice <= 0)
                    return n;
            }
            return 0;
        }
    }
}
