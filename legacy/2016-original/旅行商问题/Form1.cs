using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 旅行商问题
{
    public partial class Form1 : Form
    {
        Graphics gra;
        public Form1()
        {
            InitializeComponent();
        }
        Point[] points = new Point[100];
        int size = 0; //点的个数
        int[] order; //由0开始指回0
        int radius = 12;//圆点半径
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            drawingLineState = false;
            button3.Enabled = false;
            button7.Enabled = false;
            if (e.Button == MouseButtons.Right)
            {
                if (size > 0)
                {
                    size--;
                }
                gra.Clear(pictureBox1.BackColor);
                drawPoint();
                return;
            }
            if (size >= 100)
            {
                return;
            }

            points[size] = e.Location;
            size++;

            drawPoint();

            toolStripStatusLabel1.Text = points[size - 1].ToString();
        }

        private void drawPoint()
        {
            if (size == 0)
            {
                return;
            }
            Brush bush = new SolidBrush(Color.Orange);//起始点的填充颜色
            gra.FillEllipse(bush, points[0].X, points[0].Y, radius, radius);
            bush = new SolidBrush(Color.Green);//填充的颜色
            for (int i = 1; i < size; i++)
            {
                gra.FillEllipse(bush, points[i].X, points[i].Y, radius, radius);//画填充椭圆的方法，x坐标、y坐标、宽、高，如果是100，则半径为50
            }
        }

        private void drawLine()
        {
            int offset = radius / 2;
            Pen pen = new Pen(Color.Black);
            System.Drawing.Drawing2D.AdjustableArrowCap lineCap = new System.Drawing.Drawing2D.AdjustableArrowCap(6, 6, true);
            // pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor; //系统的这个箭头太小了
            pen.CustomEndCap = lineCap;
            Point p1 = new Point(points[0].X, points[0].Y);
            Point p2 = new Point(points[order_buff[0]].X, points[order_buff[0]].Y);
            p1.Offset(offset, offset);
            p2.Offset(offset, offset);
            gra.DrawLine(pen, p1, p2);//连接第一个起始点和序列中第一个点
            for (int i = 0; i < size - 2; i++)//少第一个起始点,少order中最后一个
            {
                p1 = new Point(points[order_buff[i]].X, points[order_buff[i]].Y);
                p2 = new Point(points[order_buff[i + 1]].X, points[order_buff[i + 1]].Y);
                p1.Offset(offset, offset);
                p2.Offset(offset, offset);
                gra.DrawLine(pen, p1, p2);
            }
            p1 = new Point(points[order_buff[size - 2]].X, points[order_buff[size - 2]].Y);
            p2 = new Point(points[0].X, points[0].Y);
            p1.Offset(offset, offset);
            p2.Offset(offset, offset);
            gra.DrawLine(pen, p1, p2);//连接序列中最后一个点和起始点
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("created by ss 2016.4.30");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            size = 0;
            gra.Clear(pictureBox1.BackColor);
            button3.Enabled = false;
            button7.Enabled = false;
        }
        GeneticAlgorithm gen;
        private bool drawingLineState = false;
        int steps;
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                steps = int.Parse(textBox1.Text);
            }
            catch { return; }
            button2.Enabled = false; button4.Enabled = false; button3.Enabled = false;
            button6.Visible = false; button7.Visible = false;
            pictureBox1.Enabled = false;
            toolStripStatusLabel1.Text = "正在进化。。。";
            Update();

            order = new int[size - 1];
            timer1.Start();
            timer2.Start();
            backgroundWorker1.RunWorkerAsync();
            drawingLineState = true;//画线状态开启，用于图像重绘
            button5.Visible = true; button5.Enabled = true;
        }
        int[] order_buff;//为防止多线程导致的画线过程中order被改变，设此项作为数据缓冲
        private void startEvolve()
        {
            gen.evolve(steps, out order); //size-1为order大小
        }

        private void showAll()
        {
            gra.Clear(pictureBox1.BackColor);
            drawPoint();

            Array.Copy(this.order, order_buff, size - 1);
            drawLine();

            double sum = 0;
            sum += gen.distance(points[0], points[order[0]]);
            for (int i = 0; i < size - 2; i++)
            {
                sum += gen.distance(points[order[i]], points[order[i + 1]]);
            }
            sum += gen.distance(points[0], points[order[size - 2]]);
            label5.Text = "当前： 遗传算法\n\n累计代数：" + gen.steps.ToString()
                + "\n\n交叉概率：" + gen.Pc.ToString("f2")
            + "\n变异概率：" + gen.Pm.ToString("f2")
            + "\n\n种群大小：" + (gen.popSize - 1).ToString()
            + "\n精英侵略强度:" + gen.NumOfAggression.ToString()
            + "\n\n总路线距离：" + sum.ToString("f2");
        }

        private void button4_Click(object sender, EventArgs e)
        {

            int i = 0;
            try
            {
                i = int.Parse(textBox4.Text);
                if (i < 2)
                {
                    MessageBox.Show("种群大小至少为2");
                    textBox4.Undo();
                    return;
                }
                if (i % 2 != 0)
                {
                    MessageBox.Show("种群大小必须为偶数");
                    textBox4.Undo();
                    return;
                }
            }
            catch (Exception err) { MessageBox.Show(err.Message); textBox4.Undo();return; }
            if (size < 2)
            {
                MessageBox.Show("至少俩个点才能开始计算。");
                return;
            }        
            try
            {
                if (int.Parse(textBox5.Text)>i)
                {
                    MessageBox.Show("种群数量不能小于侵略强度，请重新设置！");
                    return;
                }
                i++;//将偶数种群大小加1
                gen = new GeneticAlgorithm(points, size - 1, i, double.Parse(textBox2.Text), double.Parse(textBox3.Text), checkBox1.Checked, int.Parse(textBox5.Text));
            }
            catch { MessageBox.Show("参数设置有误！"); return; }
            button3.Enabled = true;
            label5.Text = "新种群已产生!";
            order_buff = new int[size - 1];//为防止多线程导致的画线过程中order被改变，设此项作为数据缓冲
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double Pm = double.Parse(textBox3.Text);
                if (Pm > 1)
                {
                    MessageBox.Show("请输入0-1之间的小数");
                    textBox3.Undo();
                    return;
                }
                gen.Pm = Pm;
            }
            catch (Exception err) { MessageBox.Show(err.Message); textBox3.Undo(); }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double Pc = double.Parse(textBox2.Text);
                if (Pc > 1)
                {
                    MessageBox.Show("请输入0-1之间的小数");
                    textBox2.Undo();
                    return;
                }
                gen.Pc = Pc;
            }
            catch (Exception err) { MessageBox.Show(err.Message); textBox2.Undo(); }

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            gen.ElitismOn = checkBox1.Checked;
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;//不检查跨线程的调用是否合法
            gra = this.pictureBox1.CreateGraphics();
            gra.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gen = new GeneticAlgorithm//此句仅仅为了防止空指针错误
                (points, 0, int.Parse(textBox4.Text), double.Parse(textBox2.Text), double.Parse(textBox3.Text), checkBox1.Checked, int.Parse(textBox5.Text));
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            gra = this.pictureBox1.CreateGraphics();
            gra.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            drawPoint();
            if (drawingLineState)
            {
                drawLine();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripProgressBar1.Value = (int)((1.0 - (double)gen.step / steps) * 1000);
            //MessageBox.Show(toolStripProgressBar1.Value.ToString());
        }
        DateTime t1, t2;
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            t1 = DateTime.Now;
            startEvolve();
            t2 = DateTime.Now;


            //if ((t2- t1).TotalSeconds>5)
            //{
            //    MessageBox.Show("完成!");
            //}

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            showAll();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            gen.cancel = true;
        }
        SimulatedAnnealing SA;
        private void button6_Click(object sender, EventArgs e) //退火降温
        {
            if (size < 2)
            {
                MessageBox.Show("至少俩个点才能开始计算。");
                return;
            }
            try
            {
                if (double.Parse(textBox8.Text) > 1 || double.Parse(textBox8.Text) < 0 || double.Parse(textBox7.Text) < 0 || double.Parse(textBox9.Text) < 0)
                {
                    MessageBox.Show("参数设置有误!\n（模拟退火的三个参数不能为负数，退火速率r范围：0<r<1）");
                    return;
                }
            }
            catch { MessageBox.Show("参数设置有误!"); return; }
            try
            {
                SA = new SimulatedAnnealing(points, size, double.Parse(textBox8.Text), double.Parse(textBox7.Text), double.Parse(textBox9.Text));
            }
            catch { MessageBox.Show("参数设置有误!"); return; }
            button7.Enabled = true;
            label5.Text = "模拟退火算法初始化完成！";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int n = 0;
            try
            {
                n = int.Parse(textBox6.Text);
            }
            catch { return; }
            toolStripStatusLabel1.Text = "正在模拟退火计算。。。";
            Update();
            order_buff = SA.run(n);

            gra.Clear(pictureBox1.BackColor);
            drawPoint();

            drawLine();

            double sum = 0;
            sum += gen.distance(points[0], points[order_buff[0]]);
            for (int i = 0; i < size - 2; i++)
            {
                sum += gen.distance(points[order_buff[i]], points[order_buff[i + 1]]);
            }
            sum += gen.distance(points[0], points[order_buff[size - 2]]);

            label5.Text = "当前： 退火降温算法\n"
                + "\n退火总次数：" + SA.steps_total.ToString()
                + "\n\n退火速率r：" + SA.r.ToString("f2")
            + "\n当前温度T：" + ((SA.T < 0.01) ? SA.T.ToString("e") : SA.T.ToString("f2"))
            + "\n\n估价差值dE：" + SA.dE.ToString("f2")
            + "\n跳跃概率(de/T)：" + SA.deT.ToString("f2")
            + "\n\n总路线距离：" + sum.ToString("f2");
            drawingLineState = true;

            toolStripStatusLabel1.Text = "模拟退火完成！";
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (int.Parse(textBox5.Text) > gen.popSize)
                {
                    MessageBox.Show("侵略强度不能大于种群数量！\n(若您已更改种群数量请生成新种群后再修改此项)");
                    textBox5.Undo();
                    return;
                }
                gen.NumOfAggression = int.Parse(textBox5.Text);
            }
            catch { textBox5.Undo(); }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            gra.Clear(pictureBox1.BackColor);
            drawPoint();
            if (drawingLineState)
            {
                drawLine();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            toolStripProgressBar1.Value = 0;

            toolStripStatusLabel1.Text = "完成！耗时：" + (t2 - t1).ToString();
            button2.Enabled = true; button4.Enabled = true; button3.Enabled = true;
            button5.Enabled = false; button5.Visible = false;
            pictureBox1.Enabled = true;
            gen.cancel = false;
            showAll();
            button6.Visible = true;
            button7.Visible = true;
        }
    }
}
