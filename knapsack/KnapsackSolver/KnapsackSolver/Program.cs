using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace KnapsackSolver {

    public class ItemReverseSortByDensity : IComparer<Item> {

        public int Compare(Item x, Item y) {
            if (x.density > y.density) return -1;
            else if (x.density < y.density) return 1;
            else return 0;
        }
    }

    public class Item {
        public int id;
        public int value;
        public int weight;
        public double density;
        public bool considerForRelaxation;
    }

    class Program {

        private static int[] value;
        private static int[] weight;
        private static int[] considerForRelaxation;
        private static double[] density;
        private static int W = 0, N = 0;
        private static int currMaxValue = 0;
        private static BitArray currMaxSelectionVars = null;

        private static Item[] items;
        private static Dictionary<int, Item> itemAt;

        static void Main4(string[] args) {

            Process currProc = Process.GetCurrentProcess();

            bool debug = true;
            //string debugInputFile = @"D:\san\discop\knapsack\data\ks_4_0";
            string debugInputFile = @"D:\san\Algo\courses\Coursera-DsicreteOptimization PascalVanHentenryck\code\knapsack\data\ks_400_0";

            string outputStr = "";
            string inputFile = "";

            if (!debug && args != null && args.Length > 0) {
                inputFile = args[0];
            }

            if (debug) {
                inputFile = debugInputFile;
            }


            if (!File.Exists(inputFile)) {
                throw new FileNotFoundException("File not found : " + inputFile);
            }

            ReadFromFile(inputFile);

            ////Console.WriteLine("N = {0}, W = {1}\n\n", N, W);

            ////considerForRelaxation = new int[N + 1];
            ////density = new double[N + 1];

            items = new Item[N];
            for (int i = 1; i <= N; i++) {
                ////considerForRelaxation[i] = 1;
                ////density[i] = (double)value[i] / weight[i];
                Item item = new Item() {
                    id = i,
                    value = value[i],
                    weight = weight[i],
                    density = (double)value[i] / weight[i],
                    considerForRelaxation = true
                };
                items[i - 1] = item;
            }

            Array.Sort(items, new ItemReverseSortByDensity());

            //Greedy approach
            currMaxSelectionVars = new BitArray(N + 1, false);
            int currMaxWt = 0;
            for (int i = 0; i < N; i++) {
                if (items[i].weight + currMaxWt <= W) {
                    currMaxWt += items[i].weight;
                    currMaxValue += items[i].value;
                    currMaxSelectionVars.Set(items[i].id, true);
                } 
            }

            //Console.WriteLine("Max Estimate = " + currMaxEstimate);
            //Console.Write("Selection Vars : ");
            Console.WriteLine(currMaxValue + " 0");
            for (int i = 1; i < currMaxSelectionVars.Count; i++) {
                bool bit = currMaxSelectionVars.Get(i);
                Console.Write(bit ? 1 : 0);
                Console.Write(" ");
            }
            Console.WriteLine();

        }

        static void Main2(string[] args) {

            Process currProc = Process.GetCurrentProcess();

            bool debug = true;
            //string debugInputFile = @"D:\san\discop\knapsack\data\ks_4_0";
            string debugInputFile = @"D:\san\discop\knapsack\data\ks_10000_0";

            string outputStr = "";
            string inputFile = "";

            if (!debug && args != null && args.Length > 0) {
                inputFile = args[0];
            }

            if (debug) {
                inputFile = debugInputFile;
            }


            if (!File.Exists(inputFile)) {
                throw new FileNotFoundException("File not found : " + inputFile);
            }

            ReadFromFile(inputFile);

            Console.WriteLine("N = {0}, W = {1}\n\n", N, W);

            ////considerForRelaxation = new int[N + 1];
            ////density = new double[N + 1];

            items = new Item[N];
            for (int i = 1; i <= N; i++) {
                ////considerForRelaxation[i] = 1;
                ////density[i] = (double)value[i] / weight[i];
                Item item = new Item() {
                    id = i,
                    value = value[i],
                    weight = weight[i],
                    density = (double)value[i] / weight[i],
                    considerForRelaxation = true
                };
                items[i-1] = item;
            }

            Array.Sort(items, new ItemReverseSortByDensity());

            itemAt = new Dictionary<int, Item>();
            for (int i = 0; i < N; i++) {
                itemAt[items[i].id] = items[i];
            }

            int E = GetRelaxedValueEstimate();
            currMaxSelectionVars = new BitArray(N+1, false);

            BranchAndBound(1, 0, W, E, currMaxSelectionVars);

            Console.WriteLine("Max Estimate = " + currMaxValue);
            Console.Write("Selection Vars : ");
            for (int i = 1; i < currMaxSelectionVars.Count; i++) {
                bool bit = currMaxSelectionVars.Get(i);
                Console.Write(bit ? 1 : 0);
            }
            Console.WriteLine();

        }


        static int GetRelaxedValueEstimate() {
            int e = 0;
            int wt = 0;
            for (int i = 0; i < N; i++) {
                if (items[i].considerForRelaxation) {
                    if (wt + items[i].weight <= W) {
                        e += items[i].value;
                        wt += items[i].weight;
                    } else {
                        int delta = W - wt;
                        e += (int)Math.Floor(items[i].density * delta);
                        break;
                    }
                }
            }

            return e;
        }

        static void Main7(string[] args) {

            Process currProc = Process.GetCurrentProcess();

            bool debug = true;
            string debugInputFile = @"D:\san\discop\knapsack\data\ks_60_0";
            //string debugInputFile = @"D:\san\discop\knapsack\data\ks_10000_0";

            string outputStr = "";
            string inputFile = "";

            if (!debug && args != null && args.Length > 0) {
                inputFile = args[0];
            }

            if (debug) {
                inputFile = debugInputFile;
            }


            if (!File.Exists(inputFile)) {
                throw new FileNotFoundException("File not found : " + inputFile);
            }

            ReadFromFile(inputFile);

            Console.WriteLine("N = {0}, W = {1}\n\n", N, W);

            ////considerForRelaxation = new int[N + 1];
            ////density = new double[N + 1];

            items = new Item[N];
            for (int i = 1; i <= N; i++) {
                ////considerForRelaxation[i] = 1;
                ////density[i] = (double)value[i] / weight[i];
                Item item = new Item() {
                    id = i,
                    value = value[i],
                    weight = weight[i],
                    density = (double)value[i] / weight[i],
                    considerForRelaxation = true
                };
                items[i - 1] = item;
            }

            Array.Sort(items, new ItemReverseSortByDensity());

            itemAt = new Dictionary<int, Item>();
            for (int i = 0; i < N; i++) {
                itemAt[items[i].id] = items[i];
            }

            //int E = GetRelaxedValueEstimate();
            currMaxSelectionVars = new BitArray(N + 1, false);

            var availability = new BitArray(N+1, true);
            var bitArray = new BitArray(N+1, false);
            var est = GetEstimate(availability);
            State root = new State() {
                availability = availability,
                bitArray = bitArray,
                estimate = est,
                id = 0,
                room = W,
                value = 0
            };
            Stack<State> st = new Stack<State>();
            st.Push(root);

            estTime.Reset();
            totTime.Reset();

            BranchAndBound1(st);

            Console.WriteLine("Max Estimate = " + currMaxValue);
            Console.Write("Selection Vars : ");
            for (int i = 1; i < currMaxSelectionVars.Count; i++) {
                bool bit = currMaxSelectionVars.Get(i);
                Console.Write(bit ? 1 : 0);
            }
            Console.WriteLine();

            Console.WriteLine(totTime.Elapsed.TotalSeconds);
            Console.WriteLine(estTime.Elapsed.TotalSeconds);

        }

        class State {
            public BitArray availability;
            public BitArray bitArray;
            public int id;
            public int value;
            public int room;
            public int estimate;
        }

        static void BranchAndBound1(Stack<State> stack ) {
            totTime.Start();
            while (stack.Count != 0) {
                var cs = stack.Peek();
                if (cs != null) {
                    int newId = cs.id + 1;
                    if (cs.id >= N || cs.room < itemAt[newId].weight) {
                        stack.Push(null);

                    } else {
                        var newBitArray = new BitArray(cs.bitArray);
                        newBitArray.Set(newId, true);
                        var newAvail = new BitArray(cs.availability);
                        var newLeftState = new State() {
                            bitArray = newBitArray,
                            id = newId,
                            availability = newAvail,
                            room = cs.room - itemAt[newId].weight,
                            value = cs.value + itemAt[newId].value,
                            estimate = cs.estimate
                        };
                        stack.Push(newLeftState);


                        if (newLeftState.value > currMaxValue) {
                            currMaxValue = newLeftState.value;
                            currMaxSelectionVars = new BitArray(newLeftState.bitArray);
                            Console.WriteLine("New max = " + currMaxValue);
                        }
                    }

                } else {
                    stack.Pop();
                    if (stack.Count > 0) {
                        var rootState = stack.Pop();
                        if (rootState == null) {
                            throw new Exception("Invalid stack state.");
                        }

                        if (rootState.id >= N) {
                            stack.Push(null);

                        } else {
                            var avail = new BitArray(rootState.availability);
                            avail.Set(rootState.id + 1, false);
                            int est = GetEstimate(avail);

                            if (est <= currMaxValue) {
                                stack.Push(null);

                            } else {

                                var newRightState = new State() {
                                    bitArray = rootState.bitArray,
                                    id = rootState.id + 1,
                                    availability = avail,
                                    room = rootState.room,
                                    value = rootState.value,
                                    estimate = est
                                };
                                stack.Push(newRightState);
                            }
                        }
                    }
                }
            }
            totTime.Stop();
        }

        static Stopwatch estTime = new Stopwatch();
        static Stopwatch totTime = new Stopwatch();


        static int GetEstimate(BitArray availability) {
            estTime.Start();
            int e = 0;
            int wt = 0;
            for (int i = 0; i < N; i++) {
                if (availability.Get(items[i].id)) {
                    if (wt + items[i].weight <= W) {
                        e += items[i].value;
                        wt += items[i].weight;
                    } else {
                        int delta = W - wt;
                        e += (int)Math.Floor(items[i].density * delta);
                        break;
                    }
                }
            }
            estTime.Stop();
            return e;
        }

        static void BranchAndBound(int i, int v, int r, int e, BitArray selectionVars) {
            ////Console.WriteLine("Branching at : " + i);
            if (e <= currMaxValue) {
                return;
            }

            if (r == 0 || i > N) {
                if (v > currMaxValue) {
                    Console.WriteLine("Max = " + v );
                    currMaxValue = v;
                    currMaxSelectionVars = new BitArray(selectionVars);
                }
                return;
            }

            if (itemAt[i].weight <= r) {
                int newV = v + itemAt[i].value;
                int newR = r - itemAt[i].weight;
                selectionVars.Set(i, true);
                BranchAndBound(i + 1, newV, newR, e, selectionVars);
                selectionVars.Set(i, false);
            }

            itemAt[i].considerForRelaxation = false;
            int newE = GetRelaxedValueEstimate();
            BranchAndBound(i + 1, v, r, newE, selectionVars);
            itemAt[i].considerForRelaxation = true;
        }


        static void Main(string[] args) {
            Process currProc = Process.GetCurrentProcess();

            bool debug = false;
            //string debugInputFile = @"D:\san\discop\knapsack\data\ks_4_0";

            string debugInputFile = @"D:\san\Algo\courses\Coursera-DsicreteOptimization PascalVanHentenryck\code\knapsack\data\ks_4_0";

            string outputStr = "";
            string inputFile = "";

            if (!debug && args != null && args.Length > 0) {
                inputFile = args[0];
            }

            if (debug) {
                inputFile = debugInputFile;
            }


            if (!File.Exists(inputFile)) {
                throw new FileNotFoundException("File not found : " + inputFile);
            }

            ReadFromFile(inputFile);

            ////Console.WriteLine("N = {0}, W = {1}\n\n", N, W);
            ////int step = N / 100;
            ////int progress = 0;


            //contains the optimal values for the selection variables 
            //paths[i] -> optimal selection variables values if we consider capacity at most i.
            //paths[i] will undergo N mutations and each time its length will increase by 1.
            ////BitArray[] prevPathsBitArray = null;/*new BitArray[W + 1];*/
            ////BitArray[] currPathsBitArray = new BitArray[W + 1];

            int[] prevIdxInA = new int[W+1];
            int[] currIdxInA = new int[W+1];

            List<BitArray> taken = new List<BitArray>();
            for (int i = 0; i <= N; i++) {
                taken.Add(new BitArray(W + 1));
            }

            //O(k,j) -> denotes the optimal knapsack value we can gather for capacity = k and items from (0..j)
            //initialize O(k,j) to 0 for j = 0. i.e if there are no items that we can consider that irrespective
            //of how much the capacity available in the knapsack is, we can only derive a max value of 0.

            ////BitArray zeroBitArray = new BitArray(N + 1, false);
            for (int i = 0; i <= W; i++) {
                currIdxInA[i] = 0;
                ////currPathsBitArray[i] = zeroBitArray;

            }

            ////Console.WriteLine("Initialization done : Memory used in MB = " + GC.GetTotalMemory(true)/1024/1024);

            for (int i = 1; i <= N ; i++) {

                ////GC.Collect();
                ////Console.WriteLine("Progress.. " + i + " out of " + N + " . Memory used in MB = " + currProc.PrivateMemorySize64 / 1024 / 1024);
                //Console.WriteLine("Progress.. " + i + " out of " + N + " . Memory used in MB = " + GC.GetTotalMemory(true)/1024/1024);

                using (StreamWriter sw = new StreamWriter("d:\\temp\\knapsack.txt", true))
                {
                    sw.WriteLine("Progress.. " + i + " out of " + N + " . Memory used in MB = " + GC.GetTotalMemory(true) / 1024 / 1024);
                }

                //we are only ever interested in A[i] and A[i-1]
                //So no need to maintain the full 2D array A[][] in memory.
                //A[i-1][] is denoted by prevIdxInA[]
                //A[i][] is denoted by currIdxInA[]
                prevIdxInA = currIdxInA;
                currIdxInA = new int[W+1];

                ////prevPaths = currPaths;
                ////currPaths = new string[W + 1];

                ////prevPathsBitArray = currPathsBitArray;
                ////currPathsBitArray = new BitArray[W + 1];

                ////if ((step > 0) && (i % step == 0)) {
                ////    Console.WriteLine("Calculation Progress = {0} %", progress++);
                ////}

                for (int x = 0; x <= W; x++) {
                    int c1 = 0, c2 = 0;
                    c1 = prevIdxInA[x];
                    if (x >= weight[i]) {
                        c2 = prevIdxInA[x - weight[i]] + value[i];
                    }

                    if (c2 > c1) {
                        currIdxInA[x] = c2;
                        ////currPathsBitArray[x] = new BitArray(prevPathsBitArray[x - weight[i]]);
                        ////currPathsBitArray[x].Set(i, true);
                        taken[i].Set(x, true);


                    } else {
                        currIdxInA[x] = c1;

                        ////currPathsBitArray[x] = prevPathsBitArray[x];
                        //taken[i].Set(x, false); //maybe it is already false? so redundant set
                    }
                }
            }


            ////Console.WriteLine("Max knapsack capacity : " + currIdxInA[W]);
            ////Console.WriteLine("==Selection variables==");
            Console.WriteLine(currIdxInA[W] + " 1");
            int currW = W;
            BitArray pathArray = new BitArray(N + 1, false);
            for (int i = N; i > 0; i--) {
                if (currW < 0) {
                    throw new Exception("Invalid trace path.");
                }
                if (taken[i].Get(currW)) {
                    pathArray.Set(i, true);
                    currW -= weight[i];
                }
            }


            for (int i = 1; i < pathArray.Count; i++) {
                bool bit = pathArray.Get(i);
                Console.Write(bit ? 1 : 0);
                Console.Write(" ");
            }
            Console.WriteLine();


            ////Console.WriteLine("== verifying correctness ==");
            ////int verifiedSum = 0;
            ////for (int i = 1; i < pathArray.Count; i++) {
            ////    if (pathArray.Get(i)) {
            ////        verifiedSum += value[i];
            ////    }
            ////}

            ////if (verifiedSum == currIdxInA[W]) {
            ////    Console.WriteLine("Verification SUCCESS !!");
            ////} else {
            ////    Console.WriteLine("Verification FAILED !!");
            ////}

            ////Console.WriteLine(outputStr);

            GC.Collect();
        }

        static void ReadFromFile(string fileName) {
            using (StreamReader sr = new StreamReader(fileName)) {
                string line = "";
                line = sr.ReadLine();
                string[] header = line.Split(' ');
                N = int.Parse(header[0]);
                W = int.Parse(header[1]);
                value = new int[N + 1];
                weight = new int[N + 1];
                int step = N / 100;
                int progress = 0;
                for (int i = 1; i < N + 1; i++) {
                    ////if ((step > 0) && (i % step == 0)) {
                    ////    Console.WriteLine("Reading input Progress : {0} %", progress++);
                    ////}
                    line = sr.ReadLine();
                    string[] lineItem = line.Split(' ');
                    value[i] = int.Parse(lineItem[0]);
                    weight[i] = int.Parse(lineItem[1]);
                }
            }
        }
    }
}
