using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Tsp {

    class Point {
        public Point(double x, double y) {
            X = x; 
            Y = y;
        }
        public double X;
        public double Y;
    }


    class Program {
        static List<Point> cities = new List<Point>();
        static HashSet<int> seenPositionsIn3Opt = new HashSet<int>();
        static int positionCacheSize = 1000000; /*cache size of 1 million*/
        static int numDecimalsAccuracy = 1;
        static bool do3Opt = false;
        static bool exploreAllNeighboursFor3Opt = false;
        static bool exploreAllNeighboursFor2Opt = false;
        static int numberOfSessions = 1000;
        static void Main(string[] args) {
            string graphFile = @"D:\san\Algo\courses\Coursera-DsicreteOptimization PascalVanHentenryck\Lectures\_Assignments\tsp\tsp\data\tsp_574_1";
            cities = ReadCityGraph(graphFile);
            //i_k_list = GenerateAll_i_k(cities.Count);
            Tsp();
        }

        static List<double[]> distVector = new List<double[]>();
        static int[] localMinimaAcrossSessions;
        static double localMinimaLenAcrossSessions = 0;

        static void Tsp() {
            //calculate the euclidean distance between every pair of cities (i,j) and fill up the value in distances array.
            Console.WriteLine("***** About to allocate " + ((double)cities.Count * cities.Count * 2)/1024/1024/1024 + " GB of memory");
            //smallDistances = new short[cities.Count, cities.Count];
            
            int numjForThisi = cities.Count;
            for (int i = 0; i < cities.Count; i++) {
                if (i % 1000 == 0) {
                    Console.WriteLine("\t Allocated space for " + i + " cities");
                }
                distVector.Add(new double[numjForThisi]);
                numjForThisi--; //because we need a diagonal distance matrix 
            }

            Console.WriteLine("Allocated distance vector.");
            for (int i = 0; i < cities.Count; i++) {
                if (i % 100 == 0) {
                    Console.WriteLine("\t Setting distance vector for city : " + i);
                    GC.Collect();
                }
                for (int j = 0; j < cities.Count; j++) {

                    if (j == i) {
                        SetDistance(i, j, double.MaxValue);
                    } else if (j > i) {
                        SetDistance(i, j, EuclideanDistAsShortMul10(cities[i], cities[j]));
                    }
                }
            }

            double initOptTourDist = double.MaxValue;
            int[] initOptTour = null;

            for (int begin = 0; begin < cities.Count; begin++) {
                
                //start the tour with city startCity.
                int startCity = begin;

                HashSet<int> included = new HashSet<int>();
                included.Add(startCity);
                int[] tour = new int[cities.Count];
                tour[0] = startCity;
                int currCity = startCity;

                for (int i = 0; i < cities.Count - 1; i++) {

                    int shortestj = -1;
                    double shortestjDistSoFar = double.MaxValue;

                    //find the nearest city to city 'currCity' that is not already included in the tour
                    for (int j = 0; j < cities.Count; j++) {

                        //we dont move from currCity to currCity
                        if (j != currCity) {

                            //if city j is not already in the tour 
                            if (!included.Contains(j)) {

                                //and if dist from i to j is shorter than shortest found so far, we set the current j as shortest j
                                if (shortestjDistSoFar > GetDistance(currCity, j)) {
                                    shortestjDistSoFar = GetDistance(currCity, j);
                                    shortestj = j;
                                }
                            }
                        }

                    }

                    if (shortestj == -1) {
                        throw new Exception("Unable to find a node to move to from : " + i);
                    }

                    included.Add(shortestj);
                    tour[i + 1] = shortestj;

                    currCity = shortestj;
                }

                double d = DistanceOfTour(tour);
                if (d < initOptTourDist) {
                    initOptTour = tour;
                    initOptTourDist = d;
                    Console.Write("Initial : ");
                    PrintTourLen(initOptTour);
                }
            }
            
            //2-opt local search heuristic : only 1 round
            int[] currOptTour = new int[initOptTour.Length];            
            Array.Copy(initOptTour, currOptTour, initOptTour.Length);
            double currOptTourLen = DistanceOfTour(currOptTour);

            int[] prevOptTour = null;
            int currRound = 1;
            int maxRoundsPerSession = 1000;
            bool foundBetterPathsInPrevRound = true;
            
            localMinimaAcrossSessions = new int[initOptTour.Length];
            Array.Copy(initOptTour, localMinimaAcrossSessions, initOptTour.Length);
            localMinimaLenAcrossSessions = DistanceOfTour(localMinimaAcrossSessions);            

            for (int s = 0; s < numberOfSessions; s++) { /*number of restarts*/

                if (currOptTourLen < localMinimaLenAcrossSessions) {
                    Array.Copy(currOptTour, localMinimaAcrossSessions, currOptTour.Length);
                    localMinimaLenAcrossSessions = currOptTourLen;
                }

                Console.WriteLine("\n***************** Shuffle : " + s + " ****************");
                if (s > 0) { /*shuffle from 2nd round*/
                    Shuffle(currOptTour);
                }

                currRound = 0;
                foundBetterPathsInPrevRound = true;

                Console.WriteLine("\t$$$$$ 2-OPT $$$$$$");
                while (foundBetterPathsInPrevRound && (currRound < maxRoundsPerSession)) {

                    prevOptTour = currOptTour;

                    Console.WriteLine("Round : " + currRound++);

                    foundBetterPathsInPrevRound = false;
                    currOptTour = TwoOpt2(prevOptTour, ref foundBetterPathsInPrevRound);
                    //currOptTour = ThreeOpt(prevOptTour, ref foundBetterPathsInPrevRound);
                    currOptTourLen = DistanceOfTour(currOptTour);
                }

                //do a few rounds of 3-opt
                if (do3Opt) {
                    foundBetterPathsInPrevRound = true;
                    currRound = 0;
                    Console.WriteLine("\t$$$$$ 3-OPT $$$$$$");
                    while (foundBetterPathsInPrevRound && (currRound < maxRoundsPerSession)) {
                        prevOptTour = currOptTour;

                        Console.WriteLine("Round : " + currRound++);

                        foundBetterPathsInPrevRound = false;
                        //currOptTour = TwoOpt2(prevOptTour, ref foundBetterPathsInPrevRound);
                        currOptTour = ThreeOpt(prevOptTour, ref foundBetterPathsInPrevRound);
                        currOptTourLen = DistanceOfTour(currOptTour);

                        int curr3OptHash = PathHash(currOptTour);
                        if (seenPositionsIn3Opt.Contains(curr3OptHash)) {
                            Console.WriteLine("\t breaking out of 3-opt because this curr opt position has alredy been explored.");
                            break;
                        }
                    }
                }
            }

            if (currOptTourLen < localMinimaLenAcrossSessions) {
                Array.Copy(currOptTour, localMinimaAcrossSessions, currOptTour.Length);
                localMinimaLenAcrossSessions = currOptTourLen;
            }

            //print the tour and the distance
            double tourLen = DistanceOfTour(localMinimaAcrossSessions);
            string path = GetTour(localMinimaAcrossSessions);

            string outFile = @"d:\temp\out.txt";
            File.WriteAllText(outFile, tourLen + " 0");
            File.AppendAllText(outFile, Environment.NewLine);
            File.AppendAllText(outFile, path);

            Console.WriteLine("\n\n == Final == ");            
            Console.WriteLine("\t" + path);
            Console.WriteLine("\t TourLen : " + tourLen);
        }

        private static double GetDistance(int i, int j) {
            if (j >= i) {
                return distVector[i][j-i];
            } else {
                return distVector[j][i-j];
            }
        }

        private static void SetDistance(int i, int j, double euclidDistAsShortx10) {
            if (j >= i) {
                distVector[i][j-i] = euclidDistAsShortx10;
            } else {
                throw new Exception("SetDistance called with i > j");
            }
        }

        private static void Shuffle(int[] tour) {
            int tourlen = tour.Length;
            Random random = new Random();
            for (int i = 0; i < tourlen; i++) {
                int target = random.Next(tourlen);

                int temp = tour[i];
                tour[i] = tour[target];
                tour[target] = temp;
            }
        }


        static List<int[]> GetAllNeighbours(int[] tour) {
            List<int[]> neighbours = new List<int[]>();
            foreach (var i_k in i_k_list) {
                neighbours.Add(TwoOptSwap(tour, i_k.Item1, i_k.Item2));
            }
            return neighbours;
        }

        static int[] TwoOpt2(int[] tour, ref bool foundBetter) {
            int tourLen = tour.Length;
            double origTourLen = DistanceOfTour(tour);
            double bestTourLenSoFar = origTourLen;
            int[] bestTourSoFar = new int[tourLen];
            int[] origTourCopy = new int[tourLen];
            Array.Copy(tour, origTourCopy, tourLen);
            Array.Copy(tour, bestTourSoFar, tourLen);
            for (int i = 0; i < tourLen; i++) {
                for (int k = i + 1; k < tourLen; k++) {
                    int[] swappedTour = TwoOptSwap(origTourCopy, i, k);
                    double distanceOfSwappedTour = DistanceOfTour(swappedTour);
                    if (distanceOfSwappedTour < bestTourLenSoFar) {
                        bestTourLenSoFar = distanceOfSwappedTour;
                        bestTourSoFar = swappedTour;
                        foundBetter = true;
                        PrintTourLen(bestTourSoFar);
                        if (!exploreAllNeighboursFor2Opt) {
                            goto end2Opt;
                        }
                    }
                }
            }
            end2Opt:
            return bestTourSoFar;
        }

        static List<Tuple<int, int>> i_k_list = new List<Tuple<int, int>>();
        static List<Tuple<int, int>> GenerateAll_i_k(int max) {
            List<Tuple<int, int>> i_k_list = new List<Tuple<int, int>>();
            for (int i = 0; i < max; i++) {
                for (int k = i + 1; k < max; k++) {
                    if (k > i) {
                        i_k_list.Add(new Tuple<int,int>(i,k));
                    }
                }
            }
            return i_k_list;
        }

        static int[] TwoOptSwap(int[] tour, int i, int k) {
            if (k <= i) {
                throw new Exception("In two opt swap, k cannot be less than i");
            }
            int[] swappedTour = new int[tour.Length];
            int sidx = 0;
            int idx = 0;

            //1. take route[0] to route[i-1] and add them in order to new_route            
            for (idx = 0; idx < i; idx++) {
                swappedTour[sidx] = tour[idx];
                sidx++;
            }

            //2. take route[i] to route[k] and add them in reverse order to new_route
            for (idx = k; idx >= i; idx--) {
                swappedTour[sidx] = tour[idx];
                sidx++;
            }

            //3. take route[k+1] to end and add them in order to new_route
            for (idx = k + 1; idx < tour.Length; idx++) {
                swappedTour[sidx] = tour[idx];
                sidx++;
            }

            return swappedTour;
        }

        static int[] TwoOpt1(int[] initialTour, ref bool foundBetter) {
            foundBetter = false;

            double bestTourLenSoFar = DistanceOfTour(initialTour);
            int[] bestTourSoFar = new int[initialTour.Length];
            Array.Copy(initialTour, bestTourSoFar, initialTour.Length);

            var neighbours = GetAllNeighbours(initialTour);
            foreach (var n in neighbours) {
                double nLen = DistanceOfTour(n);
                if (nLen < bestTourLenSoFar) {
                    foundBetter = true;

                    bestTourLenSoFar = nLen;
                    Array.Copy(n, bestTourSoFar, initialTour.Length);

                    PrintTourLen(bestTourSoFar);
                }
            }

            return bestTourSoFar;
        }

        static int[] TwoOpt(int[] initialTour, ref bool foundBetter) {

            double bestTourLenSoFar = DistanceOfTour(initialTour);
            int[] bestTourSoFar = new int[initialTour.Length];
            Array.Copy(initialTour, bestTourSoFar, initialTour.Length);

            int len = initialTour.Length;

            int tot = len - 1;
            int incr = tot/100;
            int currProg = 0;
            for (int i = 0; i < len-1; i++) {

                if (incr > 0 && i/incr > currProg) {
                    //Console.WriteLine("Progress : " + currProg++);
                }
                //Console.WriteLine("2-OPT : " + curr + "/" + tot);

                for (int j = i + 1; j < len; j++) {

                    int[] copy = new int[len];
                    Array.Copy(initialTour, copy, len);

                    //swap i and j in copy
                    int temp = copy[i];
                    copy[i] = copy[j];
                    copy[j] = temp;

                    double currLen = DistanceOfTour(copy);
                    if (currLen < bestTourLenSoFar) {
                        foundBetter = true;

                        bestTourLenSoFar = currLen;
                        Array.Copy(copy, bestTourSoFar, len);

                        PrintTourLen(bestTourSoFar);
                    }
                }
            }

            return bestTourSoFar;
        }

        static int[] BestThreeOptNeighbour(int[] path, int n3, int n6, int n8, ref double bestval, ref bool foundBetter) {
            //note : n3 < n5 < n8 
            if ((n3 >= n6) || (n6 >= n8)) {
                throw new Exception("Violation of rule : n3 < n6 < n8");
            }
            int n2 = n3 - 1;
            int n5 = n6 - 1;
            int n7 = n8 - 1;

            int len = path.Length;
            HashSet<int> filled = new HashSet<int>();

            //path1
            //n1..n2-1 : 'n2-n3' : n3+1..n5-1 : 'n5-n7' : n7-1..n6+1 : 'n6-n8' : n8+1..end
            int[] path1 = new int[len];
            int idx = 0;
            int sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path1[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path1[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n3])) {
                path1[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            for (idx = n3 + 1; idx <= n5 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path1[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n5])) {
                path1[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            if (!filled.Contains(path[n7])) {
                path1[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            for (idx = n7 - 1; idx >= n6 + 1; idx--) {
                if (!filled.Contains(path[idx])) {
                    path1[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n6])) {
                path1[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            if (!filled.Contains(path[n8])) {
                path1[sidx++] = n8;
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path1[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //path2
            //n1..n2-1 : 'n2-n5' : n5-1..n3+1 : 'n3-n7' : n7-1..n6+1 : 'n6-n8' : n8+1..end
            filled.Clear();
            int[] path2 = new int[len];
            idx = 0;
            sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path2[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path2[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n5])) {
                path2[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            for (idx = n5 - 1; idx >= n3 + 1; idx--) {
                if (!filled.Contains(path[idx])) {
                    path2[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n3])) {
                path2[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            if (!filled.Contains(path[n7])) {
                path2[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            for (idx = n7 - 1; idx >= n6 + 1; idx--) {
                path2[sidx++] = path[idx];
            }
            if (!filled.Contains(path[n6])) {
                path2[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            if (!filled.Contains(path[n8])) {
                path2[sidx++] = path[n8];
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path2[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //path3
            //n1..n2-1 : 'n2-n5' : n5-1..n3+1 : 'n3-n6' : n6+1..n7-1 : 'n7-n8' : n8+1..end
            filled.Clear();
            int[] path3 = new int[len];
            idx = 0;
            sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path3[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path3[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n5])) {
                path3[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            for (idx = n5 - 1; idx >= n3 + 1; idx--) {
                if (!filled.Contains(path[idx])) {
                    path3[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n3])) {
                path3[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            if (!filled.Contains(path[n6])) {
                path3[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            for (idx = n6 + 1; idx <= n7 - 1; idx++) {
                path3[sidx++] = path[idx];
            }
            if (!filled.Contains(path[n7])) {
                path3[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            if (!filled.Contains(path[n8])) {
                path3[sidx++] = path[n8];
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path3[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //path4
            //n1..n2-1 : 'n2-n7' : n7-1..n6+1 : 'n6-n5' : n5-1..n3+1 : 'n3-n8' : n8+1..end
            filled.Clear();
            int[] path4 = new int[len];
            idx = 0;
            sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path4[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path4[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n7])) {
                path4[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            for (idx = n7 - 1; idx >= n6 + 1; idx--) {
                if (!filled.Contains(path[idx])) {
                    path4[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n6])) {
                path4[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            if (!filled.Contains(path[n5])) {
                path4[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            for (idx = n5 - 1; idx >= n3 + 1; idx--) {
                path4[sidx++] = path[idx];
            }
            if (!filled.Contains(path[n3])) {
                path4[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            if (!filled.Contains(path[n8])) {
                path4[sidx++] = path[n8];
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path4[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //path5
            //n1..n2-1 : 'n2-n7' : n7-1..n6+1 : 'n6-n3' : n3+1..n5-1 : 'n5-n8' : n8+1..end
            filled.Clear();
            int[] path5 = new int[len];
            idx = 0;
            sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path5[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path5[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n7])) {
                path5[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            for (idx = n7 - 1; idx >= n6 + 1; idx--) {
                if (!filled.Contains(path[idx])) {
                    path5[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n6])) {
                path5[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            if (!filled.Contains(path[n3])) {
                path5[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            for (idx = n3 + 1; idx <= n5 - 1; idx++) {
                path5[sidx++] = path[idx];
            }
            if (!filled.Contains(path[n5])) {
                path5[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            if (!filled.Contains(path[n8])) {
                path5[sidx++] = path[n8];
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path5[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //path6
            //n1..n2-1 : 'n2-n6' : n6+1..n7-1 : 'n7-n3' : n3+1..n5-1 : 'n5-n8' : n8+1..end
            filled.Clear();
            int[] path6 = new int[len];
            idx = 0;
            sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path6[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path6[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n6])) {
                path6[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            for (idx = n6 + 1; idx <= n7 - 1; idx++) {
                path6[sidx++] = path[idx];
            }
            if (!filled.Contains(path[n7])) {
                path6[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            if (!filled.Contains(path[n3])) {
                path6[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            for (idx = n3 + 1; idx <= n5 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path6[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n5])) {
                path6[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            if (!filled.Contains(path[n8])) {
                path6[sidx++] = path[n8];
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path6[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //path7
            //n1..n2-1 : 'n2-n6' : n6+1..n7-1 : 'n7-n5' : n5-1..n3+1 : 'n3-n8' : n8+1..end
            filled.Clear();
            int[] path7 = new int[len];
            idx = 0;
            sidx = 0;

            for (idx = 0; idx <= n2 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path7[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n2])) {
                path7[sidx++] = path[n2];
                filled.Add(path[n2]);
            }
            if (!filled.Contains(path[n6])) {
                path7[sidx++] = path[n6];
                filled.Add(path[n6]);
            }
            for (idx = n6 + 1; idx <= n7 - 1; idx++) {
                if (!filled.Contains(path[idx])) {
                    path7[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }
            if (!filled.Contains(path[n7])) {
                path7[sidx++] = path[n7];
                filled.Add(path[n7]);
            }
            if (!filled.Contains(path[n5])) {
                path7[sidx++] = path[n5];
                filled.Add(path[n5]);
            }
            for (idx = n5 - 1; idx >= n3 + 1; idx--) {
                path7[sidx++] = path[idx];
            }
            if (!filled.Contains(path[n3])) {
                path7[sidx++] = path[n3];
                filled.Add(path[n3]);
            }
            if (!filled.Contains(path[n8])) {
                path7[sidx++] = path[n8];
                filled.Add(path[n8]);
            }
            for (idx = n8 + 1; idx < len; idx++) {
                if (!filled.Contains(path[idx])) {
                    path7[sidx++] = path[idx];
                    filled.Add(path[idx]);
                }
            }

            //take only unique paths and path != source path
            int sourceHash = PathHash(path);

            HashSet<int> pathHashes = new HashSet<int>();
            int[] bestSoFar = path;
            double bestLenSoFar = DistanceOfTour(path);
            bestval = bestLenSoFar;

            List<int[]> paths = new List<int[]>() {
                path1, path2, path3, path3, path4, path5, path6, path7
            };

            pathHashes.Add(PathHash(path));

            for (int i = 0; i < 7; i++) {
                int hash = PathHash(paths[i]);
                if (hash != sourceHash && !pathHashes.Contains(hash)) {
                    int thisPathHash = PathHash(paths[i]);
                    pathHashes.Add(thisPathHash);                                       

                    double dist = DistanceOfTour(paths[i]);
                    if (dist < bestLenSoFar) {
                        bestLenSoFar = dist;
                        bestSoFar = paths[i];
                        foundBetter = true;
                        bestval = dist;
                    }
                }
            }

            return bestSoFar;
        }

        static int PathHash(int[] path) {
            int hc = path.Length;
            for (int i = 0; i < path.Length; ++i) {
                hc = unchecked(hc * 314159 + path[i]);
            }
            return hc;
        }

        static Random random = new Random();


        static int[] ThreeOpt(int[] tour, ref bool foundBetter) {

            //compact the cache if cache is full
            if (seenPositionsIn3Opt.Count > positionCacheSize) {
                int eltToRemove = seenPositionsIn3Opt.ElementAt(random.Next(positionCacheSize));
                seenPositionsIn3Opt.Remove(eltToRemove);
            }
            //add this tour to cache - means we have explored this path
            int srcTourHash = PathHash(tour);
            seenPositionsIn3Opt.Add(srcTourHash);

            int tourLen = tour.Length;
            double origTourLen = DistanceOfTour(tour);
            double bestTourLenSoFar = origTourLen;
            int[] bestTourSoFar = new int[tourLen];
            int[] origTourCopy = new int[tourLen];
            Array.Copy(tour, origTourCopy, tourLen);
            Array.Copy(tour, bestTourSoFar, tourLen);
            for (int i = 1; i < tourLen-2; i++) {
                for (int j = i + 1; j < tourLen - 1; j++) {
                    for (int k = j + 1; k < tourLen; k++) {
                        bool betterFound = false;
                        double bestval = 0;
                        int[] bestNeighbour =  BestThreeOptNeighbour(tour, i, j, k, ref bestval, ref betterFound);
                        if (betterFound) {
                            foundBetter = true;                            
                            if (bestval < bestTourLenSoFar) {
                                bestTourLenSoFar = bestval;
                                bestTourSoFar = bestNeighbour;
                                PrintTourLen(bestTourSoFar);
                            }

                            if (!exploreAllNeighboursFor3Opt) {
                                goto end;
                            }
                        }
                    }
                }
            }
            end:
            return bestTourSoFar;
        }

        static void PrintTourLen(int[] tour) {
            Console.WriteLine(DistanceOfTour(tour) + " vs " + localMinimaLenAcrossSessions);
        }

        static void PrintTour(int[] tour) {
            Console.WriteLine(GetTour(tour));
        }

        static string GetTour(int[] tour) {
            string path = "";
            for (int i = 0; i < tour.Length; i++) {
                path += tour[i] + " ";
            }
            return path;
        }

        static double DistanceOfTour(int[] tour) {
            double tourLen = 0;
            for (int i = 0; i < tour.Length; i++) {
                //Console.Write(tour[i] + " ");
                if (i > 0) {
                    tourLen += GetDistance(tour[i - 1], tour[i]);
                }
            }

            tourLen += GetDistance(tour[tour.Length - 1], tour[0]);

            return tourLen / numDecimalsAccuracy;
        }

        private static double EuclideanDist(Point point1, Point point2) {
            return Math.Sqrt(Math.Pow((point1.X - point2.X), 2) + Math.Pow((point1.Y - point2.Y), 2));
        }

        private static double EuclideanDistAsShortMul10(Point point1, Point point2) {
            double actualDist = Math.Sqrt(Math.Pow((point1.X - point2.X), 2) + Math.Pow((point1.Y - point2.Y), 2));
            double actualDistx10 = actualDist * numDecimalsAccuracy;

            if (actualDistx10 > double.MaxValue) {
                throw new Exception("Distance > double.MaxValue");
            }
            return (double)actualDistx10;
        }

        private static List<Point> ReadCityGraph(string graphFile) {
            string[] lines = File.ReadAllLines(graphFile);
            int numCities = int.Parse(lines[0]);

            List<Point> cities = new List<Point>();
            for (int i = 1; i <= numCities; i++) {
                string[] xAndY = lines[i].Split(' ');
                double x = double.Parse(xAndY[0]);
                double y = double.Parse(xAndY[1]);

                cities.Add(new Point(x, y));
            }

            return cities;
        }
    }
}
