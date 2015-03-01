using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace vrp {
    class Point {
        public double x;
        public double y;
    }

    class NodeInfo {
        public int id;
        public Point point;
        public int demand;
    }

    class Route {
        public int truckId;
        public List<int> ids = new List<int>();
        public double totalDist;
        public int unusedDemand;
    }

    class Solution {
        public List<Route> routes = new List<Route>();
    }

    public class Solve {

        static double Dist(Point p1, Point p2) {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        static void ReadVRP(string fName) {
            string[] lines = File.ReadAllLines(fName);
            string[] header = lines[0].Split(' ');
            numNodes = int.Parse(header[0]);
            numTrucks = int.Parse(header[1]);
            truckCapacity = int.Parse(header[2]);

            string[] whStr = lines[1].Split(' ');
            wareHouseLoc = new Point() {
                x = double.Parse(whStr[1]),
                y = double.Parse(whStr[2])
            };

            /*nodes[0] always refers to the warehouse location
             the actual nodes begin from nodes[1]*/
            nodes = new NodeInfo[numNodes];
            demands = new int[numNodes];
            nodes[0] = new NodeInfo() {
                demand = 0,
                point = wareHouseLoc,
                id = 0
            };

            int currNode = 1;
            for (int i = 2; i <= numNodes; i++) {
                string[] nodeStr = lines[i].Split(' ');
                Point p = new Point() {
                    x = double.Parse(nodeStr[1]),
                    y = double.Parse(nodeStr[2])
                };
                NodeInfo n = new NodeInfo() {
                    id = currNode,
                    demand = int.Parse(nodeStr[0]),
                    point = p
                };
                demands[currNode] = n.demand;
                nodes[currNode] = n;
                currNode++;
            }

            distMatrix = new double[numNodes, numNodes];

            /*calculate the distance between every pair of nodes*/
            for (int i = 0; i < numNodes; i++) {
                for (int j = 0; j < numNodes; j++) {
                    if (i != j) {
                        distMatrix[i, j] = Dist(nodes[i].point, nodes[j].point);
                    }
                }
            }
        }

        static double[,] distMatrix = null;
        static int[] demands = null;
        static int numNodes = 0;
        static int numTrucks = 0;
        static int truckCapacity = 0;
        static Point wareHouseLoc = null;
        static NodeInfo[] nodes = null;

        #region file_names
        static string[] p = new string[] {
            "",
            "vrp_16_3_1",
            "vrp_26_8_1",
            "vrp_51_5_1",
            "vrp_101_10_1",
            "vrp_200_16_1",
            "vrp_421_41_1"
        };
        #endregion

        static string problem = p[6];
        static int maxInitialTrials = 500;
        static int LS0Count = 1000;
        static int LS1Count = 1000;
        static int LS2Count = 1000;
        static int RestartCount = 10;
        //static string problem = "vrp_5_4_1";

        public static void Main(string[] args) {

            string fName = @"D:\san\Algo\courses\Coursera-DsicreteOptimization PascalVanHentenryck\Lectures\_Assignments\vrp\vrp\data\" + problem;
            ReadVRP(fName);

            Solution bestSolnAcrossRestarts = null;
            double bestCostAcrossRestarts = double.MaxValue;
            int currLife = 0;

            while (currLife++ < RestartCount) {

                Console.WriteLine("\n \t\t LIFE " + currLife + "\n");

                Solution bestInitSoln = null;
                double bestCostInthisLife = double.MaxValue;

                #region initial_feasible_solution
                Console.WriteLine(" == Initial Feasible Solution ==");

                int currTrial = 0;
                bool feasible = false;
                while (currTrial++ < maxInitialTrials) {
                    //Console.WriteLine("\n\tTrial : " + currTrial);
                    Solution soln = GetInitialFeasibleSolution(nodes);
                    if (soln != null) {
                        //PrintSolution(soln);
                        double totalDist = GetSolutionCost(soln);

                        if (totalDist < bestCostInthisLife) {
                            bestCostInthisLife = totalDist;
                            bestInitSoln = soln;
                            //string totalDistStr = String.Format("Totaldist = {0:N}", totalDist);
                            //Console.WriteLine(totalDistStr);
                        }
                        if (currTrial % 10 == 0) {
                            string totalDistStr = String.Format("CurrTrial {0} Totaldist = {1:N} BestDist = {2:N}", currTrial, totalDist, bestCostInthisLife);
                            Console.WriteLine(totalDistStr);
                        }
                        feasible = true;
                    } else {
                        //Console.WriteLine("  ** Infeasible **");
                    }

                    Shuffle(nodes);
                }
                if (!feasible) {
                    Console.WriteLine("Could not find a feasible soln even after " + maxInitialTrials + " shuffles");
                } else {
                    ////Console.WriteLine("\n\n ** Best Solution ** ");
                    ////PrintSolution(bestSolnInThisLife);
                }
                #endregion

                Solution bestSolSoFarInThisLife = bestInitSoln;

                //== Local Searches ==

                //LS0
                Console.WriteLine("\n\t == Performing Local Search 0 ==");
                Solution ls0 = LS0(bestSolSoFarInThisLife);
                PrintSolution(ls0);
                bestSolSoFarInThisLife = ls0;

                //LS1
                Console.WriteLine("\n\t == Performing Local Search 1 ==");
                Solution ls1 = LS1(bestSolSoFarInThisLife);
                bestSolSoFarInThisLife = ls1;


                //LS2
                Console.WriteLine("\n\t == Performing Local Search 2 ==");
                Solution ls2 = LS2(bestSolSoFarInThisLife);
                bestSolSoFarInThisLife = ls2;

                bestCostInthisLife = GetSolutionCost(bestSolSoFarInThisLife);
                if (bestCostInthisLife < bestCostAcrossRestarts) {
                    bestCostAcrossRestarts = bestCostInthisLife;
                    bestSolnAcrossRestarts = bestSolSoFarInThisLife;
                }
                Console.WriteLine("\n\n\tBest cost in this life = {0:N} Best Cost Across all lifes = {1:N} ", bestCostInthisLife, bestCostAcrossRestarts + "\n");
            }

            Console.WriteLine();
            Console.WriteLine("\n == Final Solution ==");

            PrintSolution(bestSolnAcrossRestarts);

            Console.WriteLine();

            //Final solution
            PrintFinalSolution(bestSolnAcrossRestarts);
        }

        //Take pairs of points (p1,p2) from 2 trucks and try to swap them if the capacity
        //constraints are satisfied. Now perform 2-opt on both the trucks and report new neighbour
        //if the cost is improved.
        private static Solution LS2(Solution basicSoln) {
            int currLs2Count = 0;
            Solution currBestSol = basicSoln;
            bool doMore = true;
            while (doMore && currLs2Count++ < LS2Count) {
                Console.Write("\n\tLS2 : Trial " + currLs2Count + " ");
                double costSavings = 0;
                int t1 = 0, t2 = 0;
                int nodei = 0, nodej = 0;

                Solution betterSol = LS2Step(currBestSol, ref costSavings, ref t1, ref t2, ref nodei, ref nodej, true);
                if (betterSol == null) {
                    doMore = false;
                } else {
                    currBestSol = betterSol;
                    Console.Write(" : Move a point from {0} to {1} for cost savings = {2:N} :: Curr Total Cost = {3:N}", t1, t2, costSavings, GetSolutionCost(currBestSol));
                }
            }
            return currBestSol;
        }

        private static Solution LS2Step(Solution basicSoln, ref double costSavings, ref int trucki, ref int truckj, ref int nodei, ref int nodej, bool exploreAll) {
            Solution betterSol = new Solution();
            costSavings = 0;

            bool foundBetter = false;
            /*try to swap nodes pi and pj between truck_i and truck_j if the capccity constraints are satisfied*/
            Route iRoute, jRoute;
            for (int i = 0; i < numTrucks - 1; i++) {
                iRoute = basicSoln.routes[i];
                int freeCapacityIni = iRoute.unusedDemand;
                for (int j = i + 1; j < numTrucks; j++) {
                    jRoute = basicSoln.routes[j];
                    int freeCapacityInj = jRoute.unusedDemand;

                    if (iRoute.ids.Count == 0 || jRoute.ids.Count == 0) continue;

                    double origCost = iRoute.totalDist + jRoute.totalDist;

                    for (int piIdx = 0; piIdx < iRoute.ids.Count; piIdx++) {
                        int pi = iRoute.ids[piIdx];
                        if (pi == 0) continue;

                        for (int pjIdx = 0; pjIdx < jRoute.ids.Count; pjIdx++) {
                            int pj = jRoute.ids[pjIdx];
                            if (pj == 0) continue;

                            int demi = demands[pi];
                            int demj = demands[pj];

                            if ((freeCapacityIni + demi - demj >= 0) && (freeCapacityInj + demj - demi >= 0)) {
                                List<int> newiPath = new List<int>(iRoute.ids);
                                newiPath.Remove(pi);
                                newiPath.Add(pj);

                                List<int> newjPath = new List<int>(jRoute.ids);
                                newjPath.Remove(pj);
                                newjPath.Add(pi);

                                bool foundBetteri = false, foundBetterj = false;
                                double newLeni = 0, newLenj = 0;
                                int[] betteriRoute = TwoOpt(newiPath.ToArray(), ref foundBetteri, ref newLeni, false);
                                int[] betterjRoute = TwoOpt(newjPath.ToArray(), ref foundBetterj, ref newLenj, false);

                                double newCost = newLeni + newLenj;

                                double thisCostSavings = origCost - newCost;
                                if (thisCostSavings > 0.000001) {

                                    if (thisCostSavings > costSavings) {
                                        costSavings = thisCostSavings;

                                        trucki = i;
                                        truckj = j;
                                        nodei = pi;
                                        nodej = pj;

                                        foundBetter = true;

                                        Route newiRoute = new Route();
                                        if (foundBetteri && betteriRoute != null) {
                                            newiRoute.ids = betteriRoute.ToList();
                                        } else {
                                            newiRoute.ids = newiPath;
                                        }
                                        newiRoute.totalDist = newLeni;
                                        newiRoute.truckId = iRoute.truckId;
                                        newiRoute.unusedDemand = iRoute.unusedDemand + demi - demj;

                                        Route newjRoute = new Route();
                                        if (foundBetterj && betterjRoute != null) {
                                            newjRoute.ids = betterjRoute.ToList();
                                        } else {
                                            newjRoute.ids = newjPath;
                                        }
                                        newjRoute.totalDist = newLenj;
                                        newjRoute.truckId = jRoute.truckId;
                                        newjRoute.unusedDemand = jRoute.unusedDemand + demj - demi;

                                        betterSol = new Solution();
                                        //create the new solution with modified i and j routes
                                        for (int t = 0; t < basicSoln.routes.Count; t++) {
                                            if (t == i) {
                                                betterSol.routes.Add(newiRoute);
                                            } else if (t == j) {
                                                betterSol.routes.Add(newjRoute);
                                            } else {
                                                betterSol.routes.Add(basicSoln.routes[t]);
                                            }
                                        }

                                        if (!exploreAll) {
                                            goto end;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


        end:
            if (foundBetter) {
                return betterSol;
            } else {
                return null;
            }
        }

        //Take p2 from route1 and add to route2 if the capacity constraints are satisfied in route2
        //Then, via a sequence of 2-opt moves in route2 and route1, optimize both the routes.
        //if(new_dist_r2+new_dist_r1 < old_dist_r2_old_dist_r1), then we got a better neighbour
        private static Solution LS1(Solution basicSoln) {
            int currLs1Count = 0;
            Solution currBestSol = basicSoln;
            bool doMore = true;
            while (doMore && currLs1Count++ < LS1Count) {
                Console.Write("\n\tLS1 : Trial " + currLs1Count + " ");
                double costSavings = 0;
                int t1 = 0, t2 = 0;

                Solution betterSol = LS1Step(currBestSol, ref costSavings, ref t1, ref t2);
                if (betterSol == null) {
                    doMore = false;
                } else {
                    currBestSol = betterSol;
                    Console.Write(" : Move a point from {0} to {1} for cost savings = {2:N}", t1, t2, costSavings);
                }
            }
            return currBestSol;
        }
        
        private static Solution LS1Step(Solution basicSoln, ref double costSavings, ref int trucki, ref int truckj) {
            Solution betterSol = new Solution();

            bool foundBetter = false;
            /*try to move a node from truck_i to truck_j if the capccity constraints are satisfied*/
            Route iRoute, jRoute;
            for (int i = 0; i < numTrucks; i++) {
                iRoute = basicSoln.routes[i];
                for (int j = 0; j < numTrucks; j++) {
                    jRoute = basicSoln.routes[j];
                    int freeCapacityInj = jRoute.unusedDemand;

                    if (i == j) continue;
                    if (freeCapacityInj == 0) continue;
                    if (iRoute.ids.Count == 0) continue;

                    List<int> idsToProbe = iRoute.ids.Where(x => x != 0 && demands[x] <= freeCapacityInj).ToList();
                    for (int k = 0; k < idsToProbe.Count; k++) {
                        if (idsToProbe[k] != 0) { /*we cannot move the origin from i to j*/
                            int demandOfThisPointIni = demands[idsToProbe[k]];
                            if (freeCapacityInj >= demandOfThisPointIni) { /*capccity constraints are satisfied*/
                                
                                /*move ids[k] from truck_i to truck_j*/
                                List<int> newiPath = new List<int>(iRoute.ids);
                                newiPath.Remove(idsToProbe[k]); ;

                                List<int> newjPath = new List<int>(jRoute.ids);
                                newjPath.Add(idsToProbe[k]);

                                double origCost = iRoute.totalDist + jRoute.totalDist;

                                bool foundBetteri = false, foundBetterj =false;
                                double newLeni = 0, newLenj = 0;
                                int[] betteriRoute = TwoOpt(newiPath.ToArray(), ref foundBetteri, ref newLeni, false);
                                int[] betterjRoute = TwoOpt(newjPath.ToArray(), ref foundBetterj, ref newLenj, false);

                                double newCost = newLeni + newLenj;

                                costSavings = origCost-newCost;
                                if (costSavings > 0.000001) {

                                    trucki = i;
                                    truckj = j;

                                    foundBetter = true;

                                    Route newiRoute = new Route();
                                    if (foundBetteri && betteriRoute != null) {
                                        newiRoute.ids = betteriRoute.ToList();
                                    } else {
                                        newiRoute.ids = iRoute.ids;
                                    }
                                    newiRoute.totalDist = newLeni;
                                    newiRoute.truckId = iRoute.truckId;
                                    newiRoute.unusedDemand = iRoute.unusedDemand + demandOfThisPointIni;

                                    Route newjRoute = new Route();
                                    if (foundBetterj && betterjRoute != null) {
                                        newjRoute.ids = betterjRoute.ToList();
                                    } else {
                                        newjRoute.ids = jRoute.ids;
                                    }
                                    newjRoute.totalDist = newLenj;
                                    newjRoute.truckId = jRoute.truckId;
                                    newjRoute.unusedDemand = jRoute.unusedDemand - demandOfThisPointIni;

                                    //create the new solution with modified i and j routes
                                    for (int t = 0; t < basicSoln.routes.Count; t++) {
                                        if (t == i) {
                                            betterSol.routes.Add(newiRoute);
                                        } else if (t == j) {
                                            betterSol.routes.Add(newjRoute);
                                        } else {
                                            betterSol.routes.Add(basicSoln.routes[t]);
                                        }
                                    }

                                    goto end;
                                }

                            }
                        }
                    }
                }
            }


            end:
            if (foundBetter) {
                return betterSol;
            } else {
                return null;
            }
        }

        //Perform 2-OPT optimization for all the routes in the basic solution
        //return the optimized solution if one is found.
        private static Solution LS0(Solution basicSoln) {
            Solution newSol = new Solution();
            for (int i = 0; i < basicSoln.routes.Count; i++) {
                Route thisRoute = basicSoln.routes[i];
                Console.WriteLine("\tLS 0 : Truck : " + thisRoute.truckId);

                int currCount = 0;
                bool doMore = true;
                Route bestRouteSoFarForThisTruck = thisRoute;
                while (doMore && currCount++ < LS0Count) {
                    double costSavings = 0;
                    Route newRoute = TwoOpt(bestRouteSoFarForThisTruck, ref costSavings);
                    if (newRoute == null) {
                        doMore = false;
                    } else {
                        bestRouteSoFarForThisTruck = newRoute;
                    }
                }
                newSol.routes.Add(bestRouteSoFarForThisTruck);
            }
            return newSol;
        }

        //Perform a 2-opt optimization on the given route and return the new route
        private static Route TwoOpt(Route orig, ref double costSavings) {
            /*for empty trucks, we cannot find a better path*/
            if (orig.ids.Count == 0) {
                return null;
            } else {
                bool foundBetter = false;
                double newLen = 0;
                int[] betterPath = TwoOpt(orig.ids.ToArray(), ref foundBetter, ref newLen, false);

                if (foundBetter) {
                    Console.WriteLine("** Truck Id {0} : New best Route len : {1:N} Savings = {2:N}", orig.truckId, newLen, newLen - orig.totalDist);
                    costSavings = orig.totalDist - newLen;
                    return new Route() {
                        ids = betterPath.ToList(),
                        totalDist = newLen,
                        truckId = orig.truckId,
                        unusedDemand = orig.unusedDemand
                    };
                } else {
                    costSavings = 0;
                    return null;
                }
            }
        }

        static int[] TwoOpt(int[] tour, ref bool foundBetter, ref double newLen, bool exploreAllNeighboursFor2Opt) {
            int tourLen = tour.Length;
            double origTourLen = DistanceOfTour(tour);
            newLen = origTourLen;
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
                        newLen = bestTourLenSoFar;
                        bestTourSoFar = swappedTour;
                        foundBetter = true;
                        //Console.WriteLine("New best tour len : " + distanceOfSwappedTour);
                        //PrintTourLen(bestTourSoFar);
                        if (!exploreAllNeighboursFor2Opt) {
                            goto end2Opt;
                        }
                    }
                }
            }
        end2Opt:
            return bestTourSoFar;
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

        static double DistanceOfTour(int[] tour) {
            double tourLen = 0;
            for (int i = 0; i < tour.Length; i++) {
                if (i > 0) {
                    tourLen += distMatrix[tour[i - 1], tour[i]];
                }
            }
            tourLen += distMatrix[tour[tour.Length - 1], tour[0]];
            return tourLen;
        }

        static void PrintTourLen(int[] tour) {
            Console.WriteLine(DistanceOfTour(tour));
        }

        private static Solution GetInitialFeasibleSolution(NodeInfo[] nodes) {
            Solution soln = new Solution();

            int currTruck = 1;
            bool allDemandSatisfied = false;
            HashSet<int> assignedNodes = new HashSet<int>();
            while (!allDemandSatisfied && currTruck <= numTrucks) {
                int availableCap = truckCapacity;
                //iterate over each of the nodes and if any node has not been assigned yet
                //to any truck and the capacity constraints of the truck are satisfied, then
                //assign this node to this truck.
                Route thisRoute = new Route();
                thisRoute.ids.Add(0);
                thisRoute.truckId = currTruck;
                thisRoute.unusedDemand = availableCap;

                for (int i = 0; i < nodes.Length; i++) {
                    if (nodes[i].id != 0) {
                        int thisNodeId = nodes[i].id;
                        int thisNodeDemand = demands[thisNodeId];
                        if (!assignedNodes.Contains(thisNodeId) && availableCap >= thisNodeDemand) {
                            availableCap -= thisNodeDemand;
                            assignedNodes.Add(thisNodeId);

                            thisRoute.ids.Add(thisNodeId);
                            thisRoute.unusedDemand = availableCap;

                            if (assignedNodes.Count == nodes.Length - 1) {
                                allDemandSatisfied = true;
                                break;
                            }
                        }
                    }
                }
                //thisRoute.ids.Add(0); /*to keep it consistent with tsp routines*/
                CalculateRouteDistance(thisRoute);
                soln.routes.Add(thisRoute);
                currTruck++;
            }

            if (!allDemandSatisfied) {
                return null;
            } else {

                //fill up additional trucks with path : 0-0
                int usedTrucks = soln.routes.Count;
                for (int i = 0; i < numTrucks - usedTrucks; i++) {
                    soln.routes.Add(new Route() {
                        ids = new List<int>() {0},
                        totalDist = 0,
                        unusedDemand = truckCapacity,
                        truckId = currTruck++
                    });
                }

                return soln;
            }
        }

        private static void PrintSolution(Solution solution) {
            double totalDist = 0;

            StreamWriter sw = new StreamWriter("d:\\temp\\vrp\\out\\" + problem + ".txt", false);

            foreach (Route route in solution.routes) {
                string thisPath = "";
                int truckId = route.truckId;
                double thisDist = route.totalDist;
                totalDist += thisDist;
                int thisCap = truckCapacity - route.unusedDemand;

                foreach (int id in route.ids) {
                    thisPath += id + " ";
                }

                string pathStr = String.Format("TruckId = {0} Dist = {1:N} DemandSatisfied = {2} :: Path = {3}", truckId, thisDist, thisCap, thisPath);
                Console.WriteLine(pathStr);
                sw.WriteLine(pathStr);
            }

            string totalDistStr = String.Format("Totaldist = {0:N}", totalDist);
            Console.WriteLine(totalDistStr);
            sw.WriteLine(totalDistStr);

            sw.Close();
        }

        private static void PrintFinalSolution(Solution solution) {
            double totalDist = 0;

            StreamWriter sw = new StreamWriter("d:\\temp\\out.txt", false);
            string finalPath = "";
            foreach (Route route in solution.routes) {
                totalDist += route.totalDist;

                bool zeroCrossed = false;
                string part1 = "";
                string part2 = "";
                for (int i = 0; i < route.ids.Count; i++) {
                    if (route.ids[i] == 0) {
                        zeroCrossed = true;
                    } else {
                        if (zeroCrossed) {
                            part2 += route.ids[i] + " ";
                        } else {
                            part1 += route.ids[i] + " ";
                        }
                    }
                }

                string path = "0 " + part2.Trim() + " " + part1.Trim() + " 0";
                path = path.Replace("  ", " ");
                finalPath += path + "\r\n";
                
            }
            sw.WriteLine((int)totalDist + " 0");

            ////for (int i = 0; i < numTrucks - solution.routes.Count; i++) {
            ////    finalPath += "0 0\r\n";
            ////}

            sw.WriteLine(finalPath);

            sw.Close();
        }

        private static double GetSolutionCost(Solution solution) {
            double totalDist = 0;
            foreach (Route route in solution.routes) {
                totalDist += route.totalDist;
            }
            return totalDist;
        }

        private static void Shuffle(NodeInfo[] nodes) {
            Random rnd = new Random();
            int numNodes = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
                Swap(nodes, i, rnd.Next(i, numNodes));
        }

        private static void Swap(NodeInfo[] nodes, int i, int j) {
            var temp = nodes[i];
            nodes[i] = nodes[j];
            nodes[j] = temp;
        }

        private static void CalculateRouteDistance(Route route) {
            double dist = 0;
            /*from origin till last node*/
            for (int i = 0; i < route.ids.Count - 1; i++) {
                dist += distMatrix[route.ids[i], route.ids[i + 1]];
            }

            dist += distMatrix[route.ids[route.ids.Count - 1], route.ids[0]]; /*add last node to origin*/
            route.totalDist = dist;
        }
    }

}
