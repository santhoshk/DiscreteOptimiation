using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace facility {

    class Point {
        public double x;
        public double y;
    }

    class Warehouse {
        public int wid;
        
        public Point point;
        public double setupCost;
        public int capacity;
    }

    class WareHouseCustomerDistance {
        public int cid;
        public int wid;
        public double distance;
    }

    class Customer {
        public int cid;

        public Point point;
        public int demand;
    }

    class WarehouseCustDistComparer : IComparer<WareHouseCustomerDistance> {
        public int Compare(WareHouseCustomerDistance x, WareHouseCustomerDistance y) {
            if (x.distance < y.distance) {
                return -1;
            } else if (y.distance < x.distance) {
                return 1;
            } else {
                return 0;
            }
        }
    }

    class Program {

        static Dictionary<int, List<WareHouseCustomerDistance>> cidToWarehouseDistMap = new Dictionary<int, List<WareHouseCustomerDistance>>();
        static Dictionary<int, List<WareHouseCustomerDistance>> widToCustomerDistMap = new Dictionary<int, List<WareHouseCustomerDistance>>();
        
        //todo : consider replacing this with 1 array, namely the demand for each customer indexed by cid.
        static Dictionary<int, Customer> cidToCustomerMap = new Dictionary<int, Customer>();
        
        //todo : consider replacing this with 2 arrays, one for setup cost and one for capacity, both indexed by wids
        static Dictionary<int, Warehouse> widToWarehouseMap = new Dictionary<int, Warehouse>();

        static double[,] cidTowidDist = null;
        //static double[] setupCostFor = null;

        static Dictionary<int, HashSet<int>> openWidToCidsMap = new Dictionary<int,HashSet<int>>();
        static HashSet<int> widsUsed = null;

        static int n;
        static int m;

        //|N| |M|
        //s_0 cap_0 x_0 y_0
        //s_1 cap_1 x_1 y_1
        //...
        //s_|N|-1 cap_|N|-1 x_|N|-1 y_|N|-1
        //d_|N| x_|N| y_|N|
        //d_|N|+1 x_|N|+1 y_|N|+1
        //...
        //d_|N|+|M|-1 x_|N|+|M|-1 y_|N|+|M|-1
        static void ReadInput(string filePath) {
            string[] lines = File.ReadAllLines(filePath);
            var n_m = lines[0].Split(' ');
            n = int.Parse(n_m[0]);
            m = int.Parse(n_m[1]);

            //read warehouses
            for (int i = 1; i <= n; i++) {
                var n_parse = lines[i].Split(' ');
                double setup = double.Parse(n_parse[0]);
                int cap = int.Parse(n_parse[1]);
                double x = double.Parse(n_parse[2]);
                double y = double.Parse(n_parse[3]);

                Point p = new Point() {x = x, y = y};

                Warehouse w = new Warehouse() {
                    capacity = cap,
                    point = p,
                    setupCost = setup,
                    wid = i-1
                };

                widToWarehouseMap[w.wid] = w;
                widToCustomerDistMap[w.wid] = new List<WareHouseCustomerDistance>();
            }

            //read customers
            for (int i = n + 1; i <= n + m; i++) {
                var m_parse = lines[i].Split(' ');
                int dem = int.Parse(m_parse[0]);
                double x = double.Parse(m_parse[1]);
                double y = double.Parse(m_parse[2]);

                Point p = new Point() { x = x, y = y };
                Customer c = new Customer() {
                    cid = i-n-1,
                    demand = dem,
                    point = p
                };
                cidToCustomerMap[c.cid] = c;
                cidToWarehouseDistMap[c.cid] = new List<WareHouseCustomerDistance>();
            }

            //fill up the cidToWarehouseDist map and the cidTowidDist 2-d array
            //the List<WarehouseDist> should be sorted in ascending order by distance
            cidTowidDist = new double[m, n];
            foreach (var kvp_c in cidToCustomerMap) {
                int cid = kvp_c.Key;
                //cidToWarehouseDistMap[cid] = new List<WareHouseCustomerDistance>();
                foreach (var kvp_w in widToWarehouseMap) {
                    int wid = kvp_w.Key;
                    double dist = Dist(kvp_c.Value.point, kvp_w.Value.point);

                    WareHouseCustomerDistance wcd = new WareHouseCustomerDistance() {
                        cid = cid, wid = wid, distance = dist
                    };

                    cidTowidDist[cid, wid] = dist;
                    cidToWarehouseDistMap[cid].Add(wcd);
                    widToCustomerDistMap[wid].Add(wcd);
                }

                ////now sort by ascending order of distances
                //cidToWarehouseDistMap[cid].Sort(new WarehouseCustDistComparer());
            }

            //now sort by ascending order of distances
            foreach (var kvp in cidToWarehouseDistMap) {
                kvp.Value.Sort(new WarehouseCustDistComparer());
            }
            foreach (var kvp in widToCustomerDistMap) {
                kvp.Value.Sort(new WarehouseCustDistComparer());
            }
        }

        static double Dist(Point p1, Point p2) {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        //gets the assignment cost for this configuration of cids assigned to wids
        static double GetAssignmentCost(int[] cidTowidMap) {
            double cost = 0;
            int[] accountedWarehouses = new int[n];
            for (int i = 0; i < m; i++) {
                int wid = cidTowidMap[i];
                cost += (1 - accountedWarehouses[wid]) * widToWarehouseMap[wid].setupCost;
                accountedWarehouses[wid] = 1;
                cost += cidTowidDist[i,wid];
            }
            return cost;
        }

        static HashSet<int> exploredHash = new HashSet<int>();
        static int linearPruneCount = 0;
        static bool pruningEnabled = false;

        static int maxRestarts;
        static int maxTrial1; /*LS1 : Take any customer ci : wj. iso wj, search for wk, that has a lower cost*/
        static int maxTrial2; /*LS2 : Take any pair c1-w1; c2-w2. Try to assign then to n different warehouses choosing the smallest possible cost*/
        static int maxTrial3; /*LS3 : Take any pair c1-w1; c2-w2. Try to assign then to np2 different combinations of warehouses choosing the smallest possible cost*/
        static int maxTrial4; /*LS4 : Take any pair c1-w1; c2-w2. Try to swap them choosing the smallest possible cost. */
        static int maxTrial5; /*LS5 : Take any triplet c1-w1; c2-w2; c3-w3. Try to swap them choosing the smallest possible cost. */
        static int maxTrialOpenCloseFacility; /*LS_OpenClose : open and close facilities trying to minimize total cost*/

        static int globalLoopCount;

        static int thresholdForLS4 = 33000000;

        static void Main(string[] args) {
            string filePath = @"D:\san\Algo\courses\Coursera-DsicreteOptimization PascalVanHentenryck\Lectures\_Assignments\facility\facility\data\fl_500_7";
            maxRestarts = 1000;
            maxTrial1 = 6000;
            maxTrial2 = 0;
            maxTrial3 = 0;
            maxTrial4 = 200;
            maxTrial5 = 0;
            maxTrialOpenCloseFacility = 100;
            globalLoopCount = 10;

            ReadInput(filePath);
            Facility();
        }

        static void GetOriginalRemainingCapacity(Dictionary<int, int> remainingCapacityFor) {
            for (int i = 0; i < n; i++) {
                remainingCapacityFor[i] = widToWarehouseMap[i].capacity;
            }
        }

        static void Facility() {

            int[] cidTowidMap = new int[m];
            int[] widTocidCount = new int[n];
            int[] assignedWarehouses = new int[n]; //by default all values are 0, so no need to initialize

            Dictionary<int, int> remainingCapacityFor = new Dictionary<int, int>();//remaining capacity for each warehouse
            GetOriginalRemainingCapacity(remainingCapacityFor);

            #region Initial_Assignment_2.1
            //////Initial assignment 2.1
            //////Assign each customer to the nearest warehouse s.t 
            //////the capacity of the warehouse has not been exceeded
            ////for (int c = 0; c < m; c++) {
            ////    Console.WriteLine("processing customer : " + c + " out of " + m);
            ////    //find next free warehouse for this customer
            ////    Customer thisCustomer = cidToCustomerMap[c];

            ////    //get the warehouse distances in ascending sorted order, so the nearest warehouse is probed first.
            ////    List<WareHouseCustomerDistance> allWarehouseDist = cidToWarehouseDistMap[c];
            ////    for (int i = 0; i < allWarehouseDist.Count; i++) {
            ////        int wid = allWarehouseDist[i].wid;
            ////        if (remainingCapacityFor[wid] >= thisCustomer.demand) {
            ////            cidTowidMap[thisCustomer.cid] = allWarehouseDist[i].wid;
            ////            remainingCapacityFor[wid] -= thisCustomer.demand;
            ////            assignedWarehouses[wid] = 1;
            ////            widTocidCount[wid]++; 
            ////            break;
            ////        }
            ////    }
            ////}
            #endregion

            #region Initial_Assignment_2.3
            //////Initial assignment 2.3
            //////Assign each customer to the nearest warehouse s.t 
            //////process in increasing order of c-w dist + capacity constraints
            ////List<WareHouseCustomerDistance> allCidWidDists = new List<WareHouseCustomerDistance>();
            ////for (int c = 0; c < m; c++) {
            ////    allCidWidDists.AddRange(cidToWarehouseDistMap[c]);
            ////}
            ////HashSet<int> assignedCids = new HashSet<int>();
            ////allCidWidDists.Sort(new WarehouseCustDistComparer());
            ////for (int i = 0; i < allCidWidDists.Count; i++) {
            ////    var cidWidDist = allCidWidDists[i];
            ////    int cid = cidWidDist.cid;
            ////    int wid = cidWidDist.wid;
            ////    double dist = cidWidDist.distance;
            ////    if (!assignedCids.Contains(cid) && remainingCapacityFor[wid] >= cidToCustomerMap[cid].demand) {
            ////        assignedCids.Add(cid);
            ////        cidTowidMap[cid] = wid;
            ////        remainingCapacityFor[wid] -= cidToCustomerMap[cid].demand;
            ////        assignedWarehouses[wid] = 1;
            ////        widTocidCount[wid]++;
            ////        if (assignedCids.Count == m) {
            ////            break;
            ////        }
            ////    }
            ////}
            ////int changes = 0;
            ////for (int i = 0; i < allCidWidDists.Count; i++) {
            ////    var cidWidDist = allCidWidDists[i];
            ////    int cid = cidWidDist.cid;
            ////    int wid = cidWidDist.wid;
            ////    double dist = cidWidDist.distance;

            ////    int origAssignedWid = cidTowidMap[cid];

            ////    double costSavingsOnRearrangement = 0;
            ////    if (cidTowidMap[cid] != wid && remainingCapacityFor[wid] >= cidToCustomerMap[cid].demand) {
            ////        if (assignedWarehouses[wid] == 0) {
            ////            costSavingsOnRearrangement -= widToWarehouseMap[wid].setupCost;
            ////        }
            ////        if (widTocidCount[origAssignedWid] == 1) {
            ////            costSavingsOnRearrangement += widToWarehouseMap[origAssignedWid].setupCost;
            ////        }

            ////        costSavingsOnRearrangement -= cidTowidDist[cid, wid];
            ////        costSavingsOnRearrangement += cidTowidDist[cid, origAssignedWid];

            ////        if (costSavingsOnRearrangement > 0.00000001) {
            ////            changes++;
            ////            if (widTocidCount[origAssignedWid] == 1) {
            ////                assignedWarehouses[wid] = 0;
            ////            }
            ////            cidTowidMap[cid] = wid;
            ////            remainingCapacityFor[wid] -= cidToCustomerMap[cid].demand;
            ////            remainingCapacityFor[origAssignedWid] += cidToCustomerMap[cid].demand;
            ////            assignedWarehouses[wid] = 1;
            ////            widTocidCount[wid]++;
            ////            widTocidCount[origAssignedWid]--;
            ////        }
            ////    }
            ////}

            ////Console.WriteLine("** Changes = " + changes);
            #endregion

            #region InitialAssignment 2.2
            //////Initial assignment 2.2
            //////assign each customer (from 1..m) to a warehouse such the total cost is minimized
            //////greedily consider only the current customer while assigning a warehouse            
            
            ////for (int c = 0; c < m; c++) {
            ////    //find the ware house for customer 'c' s.t. the total cost added is minimal
            ////    Customer thisCust = cidToCustomerMap[c];

            ////    List<WareHouseCustomerDistance> custWareHouseDist = cidToWarehouseDistMap[c];
            ////    double bestCostForThisCust = int.MaxValue;
            ////    int selectedWarehouse = -1;

            ////    for (int w = 0; w < custWareHouseDist.Count; w++) {
                    
            ////        double thisWarehouseCost = double.MaxValue;
                    
            ////        int thisWid = custWareHouseDist[w].wid;

            ////        if (custWareHouseDist[w].distance > bestCostForThisCust) {
            ////            //from now on, whatever we select can only be greater then our current best cost, so simply break out.
            ////            break;
            ////        }

            ////        //this warehouse has enough capacity for this cust.
            ////        if (remainingCapacityFor[thisWid] >= thisCust.demand) {

            ////            //this warehouse is already not included, then include the setup cost also
            ////            if (!assignedWarehouses.Contains(thisWid)) {                            
            ////                thisWarehouseCost += widToWarehouseMap[thisWid].setupCost;                            
            ////            } 
                        
            ////            //add the distance cost to the cost of selecting this warehouse for this customer.
            ////            thisWarehouseCost += custWareHouseDist[w].distance;                        
            ////        }

            ////        if (thisWarehouseCost < bestCostForThisCust) {
            ////            selectedWarehouse = w;
            ////            bestCostForThisCust = thisWarehouseCost;
            ////        }
            ////    }

            ////    assignedWarehouses[selectedWarehouse] = 1;
            ////    remainingCapacityFor[selectedWarehouse] -= thisCust.demand;
            ////    cidTowidMap[c] = selectedWarehouse;
            ////    widTocidCount[selectedWarehouse]++; //one more customer assigned to this warehouse
            ////}
            #endregion

            ////2.3 Initial assignment, simply assign each customer to a random warehouse
            ////provided that the warehouse capacity constraints are satisfied
            ////If after a few attempts, we are not able to assign any warehouse for 
            ////customer c, then linearly probe all the warehouses for that customer and 
            ////assign the first warehouse that satisfies the capacity constraints.

            double cost = double.MaxValue;
            cost = GetAssignmentCost(cidTowidMap);
            Console.WriteLine("** Initial cost assignment = " + cost);

            //Restarts
            int currRestart = 0;

            int[] bestCidToWidMapAcrossRestarts = new int[m];
            double bestCostSoFarAcrossRestarts = cost;
            Array.Copy(cidTowidMap, bestCidToWidMapAcrossRestarts, m);

            PrintCost("\n Initial Cost : ", cidTowidMap);

            while (currRestart++ < maxRestarts) {

                var memoryInMb = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;
                Console.WriteLine("\n*** Restart : " + currRestart + " :: Memory used in MB = " + memoryInMb + " :: Number of prunes = " + linearPruneCount);

                //if (currRestart > 1) {
                    AssignWarehousesRandomly(cidTowidMap, widTocidCount, assignedWarehouses, remainingCapacityFor);
                //}

                cost = GetAssignmentCost(cidTowidMap);

                int[] currCidToWidMap = new int[m];
                double currBestCostSoFar = cost;
                Array.Copy(cidTowidMap, currCidToWidMap, m);

                int currGlobalLoop = 0;
                bool foundChangesInPrevLoop = true;
                while (foundChangesInPrevLoop && currGlobalLoop++ < globalLoopCount) {

                    foundChangesInPrevLoop = false;

                    Console.WriteLine("****** Global Loop Count = " + currGlobalLoop);

                    //LocalSearch
                    //LS1 : Take any customer ci : wj
                    //iso wj, search for wk, that has a lower cost
                    //after assigning a suitable wk, if wj has no customers, then remove the setup cost for wj            
                    int cidToChange;
                    int widToChangeTo;

                    int currTrial = 0;

                    Console.WriteLine("\t **** LS1");
                    bool continueLs1 = true;
                    while (continueLs1 && currTrial++ < maxTrial1) {
                        continueLs1 = false;
                        ////if (currTrial % 100 == 0) {
                        ////    currBestCostSoFar = GetAssignmentCost(currCidToWidMap);
                        ////    Console.WriteLine("\t LS1 : Trial " + currTrial + " : " + currBestCostSoFar);
                        ////}

                        Stack<LS1Swap> ls1SwapList = new Stack<LS1Swap>();
                        FindBetterNeighbour1(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out cidToChange, out widToChangeTo, ls1SwapList);
                        if (ls1SwapList.Count == 0) {
                            break;
                        } else {

                            while (ls1SwapList.Count > 0) {

                                var currSwap = ls1SwapList.Pop();

                                cidToChange = currSwap.ci;
                                widToChangeTo = currSwap.wk;

                                double costSavings = 0;
                                int widToChangeFrom = currCidToWidMap[cidToChange];

                                if (widTocidCount[widToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                    costSavings += widToWarehouseMap[widToChangeFrom].setupCost;
                                }
                                costSavings += cidTowidDist[cidToChange, widToChangeFrom];
                                
                                if (widTocidCount[widToChangeTo] == 0) { /*we are the fist to use this wid, so add setup cost*/
                                    costSavings -= widToWarehouseMap[widToChangeTo].setupCost;
                                }
                                costSavings -= cidTowidDist[cidToChange, widToChangeTo];

                                if (costSavings > 0 && remainingCapacityFor[widToChangeTo] >= cidToCustomerMap[cidToChange].demand) {

                                    foundChangesInPrevLoop = true;
                                    continueLs1 = true;

                                    //we've found a better neighbour, rearrange the assignment list

                                    //unplug
                                    if (widTocidCount[widToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                        assignedWarehouses[widToChangeFrom] = 0;
                                    }
                                    widTocidCount[widToChangeFrom]--;
                                    remainingCapacityFor[widToChangeFrom] += cidToCustomerMap[cidToChange].demand;

                                    //plug
                                    if (widTocidCount[widToChangeTo] == 0) { /*we are the fist to use this wid, so add setup cost*/
                                        assignedWarehouses[widToChangeTo] = 1;
                                    }
                                    widTocidCount[widToChangeTo]++;
                                    remainingCapacityFor[widToChangeTo] -= cidToCustomerMap[cidToChange].demand;

                                    currCidToWidMap[cidToChange] = widToChangeTo;
                                    /////*now we will explore currCidToWidMap, so add this map to the list of explored paths
                                    //// because we do not want to explore this exact same path again. thats a waste of time.*/
                                    ////int hash = GetAssignmentHash(currCidToWidMap);
                                    ////exploredHash.Add(hash);

                                    //PrintCost("\t\tCost savings = " + costSavings, currCidToWidMap);
                                    ////PrintCost("\t\t" + bestCostSoFarAcrossRestarts + "--", currCidToWidMap);
                                }
                            }
                            currBestCostSoFar = GetAssignmentCost(currCidToWidMap);
                            Console.WriteLine("\t LS1 : Cost :" + currBestCostSoFar);
                        }
                    }

                    Console.WriteLine("\t\t *** LS : Open-Close Facility");
                    currTrial = 0;
                    while (currTrial++ < maxTrialOpenCloseFacility) {
                        bool foundBetter;
                        FindBetterNeighbourFacilityFlipFlop(ref currCidToWidMap, ref widTocidCount, ref assignedWarehouses, ref remainingCapacityFor, out foundBetter);
                        if (!foundBetter) {
                            break;
                        } else {
                            Console.WriteLine("\t\t *** Open-Close : CurrCost = " + GetAssignmentCost(currCidToWidMap));
                        }
                    }


                    Console.WriteLine("\t\t **** LS2");
                    currTrial = 0;
                    while (currTrial++ < maxTrial2) {

                        if (currTrial % 1 == 0) {
                            currBestCostSoFar = GetAssignmentCost(currCidToWidMap);
                            //Console.Write("\n\t\t LS2 : Trial " + currTrial + " : " + currBestCostSoFar);
                        }

                        int c1CidToChange, c2CidToChange;
                        List<LS2Swap> ls2List = new List<LS2Swap>();
                        ////FindBetterNeighbour2(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out c1CidToChange, out c2CidToChange, out widToChangeTo, ls2List);
                        FindBetterNeighbour22(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out c1CidToChange, out c2CidToChange, out widToChangeTo, ls2List);
                        ////FindBetterNeighbour21(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out c1CidToChange, out c2CidToChange, out widToChangeTo, ls2List);
                        if (ls2List.Count == 0) {
                            break;
                        } else {

                            for (int s = 0; s < ls2List.Count; s++) {

                                LS2Swap thisSwap = ls2List[s];
                                c1CidToChange = thisSwap.c1;
                                c2CidToChange = thisSwap.c2;
                                widToChangeTo = thisSwap.wk;

                                if (remainingCapacityFor[widToChangeTo] >= cidToCustomerMap[c1CidToChange].demand + cidToCustomerMap[c2CidToChange].demand) {

                                    //we've found a better neighbour, rearrange the assignment list
                                    int w1WidToChangeFrom = currCidToWidMap[c1CidToChange];
                                    int w2WidToChangeFrom = currCidToWidMap[c2CidToChange];
                                    double costSavings = 0;

                                    if (widTocidCount[w1WidToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                        costSavings += widToWarehouseMap[w1WidToChangeFrom].setupCost;
                                    }
                                    costSavings += cidTowidDist[c1CidToChange, w1WidToChangeFrom];
                                    if (widTocidCount[w2WidToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                        costSavings += widToWarehouseMap[w2WidToChangeFrom].setupCost;
                                    }
                                    costSavings += cidTowidDist[c2CidToChange, w2WidToChangeFrom];

                                    if (widTocidCount[widToChangeTo] == 0) { /*we are the fist to use this wid, so add setup cost*/
                                        costSavings -= widToWarehouseMap[widToChangeTo].setupCost;
                                    }
                                    costSavings -= cidTowidDist[c1CidToChange, widToChangeTo];
                                    costSavings -= cidTowidDist[c2CidToChange, widToChangeTo];

                                    if (costSavings > 0) {

                                        foundChangesInPrevLoop = true;

                                        //unplug 1
                                        if (widTocidCount[w1WidToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                            assignedWarehouses[w1WidToChangeFrom] = 0;
                                        }
                                        widTocidCount[w1WidToChangeFrom]--;
                                        remainingCapacityFor[w1WidToChangeFrom] += cidToCustomerMap[c1CidToChange].demand;

                                        //unplug 2
                                        if (widTocidCount[w2WidToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                            assignedWarehouses[w2WidToChangeFrom] = 0;
                                        }
                                        widTocidCount[w2WidToChangeFrom]--;
                                        remainingCapacityFor[w2WidToChangeFrom] += cidToCustomerMap[c2CidToChange].demand;

                                        //plug 1 and 2
                                        if (widTocidCount[widToChangeTo] == 0) { /*we are the fist to use this wid, so add setup cost*/
                                            assignedWarehouses[widToChangeTo] = 1;
                                        }
                                        widTocidCount[widToChangeTo] += 2;
                                        remainingCapacityFor[widToChangeTo] -= cidToCustomerMap[c1CidToChange].demand;
                                        remainingCapacityFor[widToChangeTo] -= cidToCustomerMap[c2CidToChange].demand;

                                        currCidToWidMap[c1CidToChange] = widToChangeTo;
                                        currCidToWidMap[c2CidToChange] = widToChangeTo;

                                        /////*now we will explore currCidToWidMap, so add this map to the list of explored paths
                                        //// because we do not want to explore this exact same path again. thats a waste of time.*/
                                        ////int hash = GetAssignmentHash(currCidToWidMap);
                                        ////exploredHash.Add(hash);

                                        PrintCost("\t\t  LS2 Cost savings = " + costSavings, currCidToWidMap);
                                        ////PrintCost("\t\t" + bestCostSoFarAcrossRestarts + "--", currCidToWidMap);
                                    }
                                }
                            }
                        }
                    }

                    #region LS3
                    Console.WriteLine("\t\t\t **** LS3");
                    currTrial = 0;
                    while (currTrial++ < maxTrial3) {

                        //if (currTrial % 1 == 0) {
                        //    currBestCostSoFar = GetAssignmentCost(currCidToWidMap);
                        //    Console.Write("\n\t\t\t LS3 : Trial " + currTrial + " : " + currBestCostSoFar);
                        //}

                        int c1CidToChange, c2CidToChange;
                        int w1WidToChangeTo, w2WidToChangeTo;
                        FindBetterNeighbour3(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out c1CidToChange, out c2CidToChange, out w1WidToChangeTo, out w2WidToChangeTo);
                        if (c1CidToChange == -1 || c2CidToChange == -1) {
                            break;
                        } else {

                            foundChangesInPrevLoop = true;

                            //we've found a better neighbour, rearrange the assignment list
                            int w1WidToChangeFrom = currCidToWidMap[c1CidToChange];
                            int w2WidToChangeFrom = currCidToWidMap[c2CidToChange];
                            double costSavings = 0;

                            //unplug 1
                            if (widTocidCount[w1WidToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                costSavings += widToWarehouseMap[w1WidToChangeFrom].setupCost;
                                assignedWarehouses[w1WidToChangeFrom] = 0;
                            }
                            costSavings += cidTowidDist[c1CidToChange, w1WidToChangeFrom];
                            widTocidCount[w1WidToChangeFrom]--;
                            remainingCapacityFor[w1WidToChangeFrom] += cidToCustomerMap[c1CidToChange].demand;

                            //unplug 2
                            if (widTocidCount[w2WidToChangeFrom] == 1) { /*only this cid has been assigned to this wid, so remove setup cost also*/
                                costSavings += widToWarehouseMap[w2WidToChangeFrom].setupCost;
                                assignedWarehouses[w2WidToChangeFrom] = 0;
                            }
                            costSavings += cidTowidDist[c2CidToChange, w2WidToChangeFrom];
                            widTocidCount[w2WidToChangeFrom]--;
                            remainingCapacityFor[w2WidToChangeFrom] += cidToCustomerMap[c2CidToChange].demand;

                            //plug 1 and 2
                            if (widTocidCount[w1WidToChangeTo] == 0) { /*we are the fist to use this wid, so add setup cost*/
                                costSavings -= widToWarehouseMap[w1WidToChangeTo].setupCost;
                                assignedWarehouses[w1WidToChangeTo] = 1;
                            }
                            if (widTocidCount[w2WidToChangeTo] == 0) { /*we are the fist to use this wid, so add setup cost*/
                                costSavings -= widToWarehouseMap[w2WidToChangeTo].setupCost;
                                assignedWarehouses[w2WidToChangeTo] = 1;
                            }
                            costSavings -= cidTowidDist[c1CidToChange, w1WidToChangeTo];
                            costSavings -= cidTowidDist[c2CidToChange, w2WidToChangeTo];
                            widTocidCount[w1WidToChangeTo]++;
                            widTocidCount[w2WidToChangeTo]++;
                            remainingCapacityFor[w1WidToChangeTo] -= cidToCustomerMap[c1CidToChange].demand;
                            remainingCapacityFor[w2WidToChangeTo] -= cidToCustomerMap[c2CidToChange].demand;

                            currCidToWidMap[c1CidToChange] = w1WidToChangeTo;
                            currCidToWidMap[c2CidToChange] = w2WidToChangeTo;

                            ///*now we will explore currCidToWidMap, so add this map to the list of explored paths
                            // because we do not want to explore this exact same path again. thats a waste of time.*/
                            //int hash = GetAssignmentHash(currCidToWidMap);
                            //exploredHash.Add(hash);

                            PrintCost("\n\t\t\t  ** LS3 Cost savings = " + costSavings, currCidToWidMap);
                            ////PrintCost("\t\t" + bestCostSoFarAcrossRestarts + "--", currCidToWidMap);
                        }
                    }
                    #endregion

                    #region LS4
                    Console.WriteLine("\t\t\t\t **** LS4");
                    currTrial = 0;
                    cost = GetAssignmentCost(currCidToWidMap);
                    if (cost < thresholdForLS4) {
                        while (currTrial++ < maxTrial4) {

                            //if (currTrial % 1 == 0) {
                            //    currBestCostSoFar = GetAssignmentCost(currCidToWidMap);
                            //    Console.Write("\n\t\t\t LS3 : Trial " + currTrial + " : " + currBestCostSoFar);
                            //}

                            int c1CidToChange, c2CidToChange;
                            int w1WidToChangeTo, w2WidToChangeTo;
                            FindBetterNeighbour4(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out c1CidToChange, out c2CidToChange, out w1WidToChangeTo, out w2WidToChangeTo);
                            if (c1CidToChange == -1 || c2CidToChange == -1) {
                                break;
                            } else {

                                //we've found a better neighbour, rearrange the assignment list
                                int w1WidToChangeFrom = w2WidToChangeTo;
                                int w2WidToChangeFrom = w1WidToChangeTo;
                                double costSavings = 0;

                                //unplug 1
                                costSavings += cidTowidDist[c1CidToChange, w1WidToChangeFrom];
                                remainingCapacityFor[w1WidToChangeFrom] += cidToCustomerMap[c1CidToChange].demand;

                                //unplug 2
                                costSavings += cidTowidDist[c2CidToChange, w2WidToChangeFrom];
                                remainingCapacityFor[w2WidToChangeFrom] += cidToCustomerMap[c2CidToChange].demand;

                                //plug 1 and 2
                                costSavings -= cidTowidDist[c1CidToChange, w1WidToChangeTo];
                                costSavings -= cidTowidDist[c2CidToChange, w2WidToChangeTo];
                                remainingCapacityFor[w1WidToChangeTo] -= cidToCustomerMap[c1CidToChange].demand;
                                remainingCapacityFor[w2WidToChangeTo] -= cidToCustomerMap[c2CidToChange].demand;

                                currCidToWidMap[c1CidToChange] = w1WidToChangeTo;
                                currCidToWidMap[c2CidToChange] = w2WidToChangeTo;

                                ///*now we will explore currCidToWidMap, so add this map to the list of explored paths
                                // because we do not want to explore this exact same path again. thats a waste of time.*/
                                //int hash = GetAssignmentHash(currCidToWidMap);
                                //exploredHash.Add(hash);

                                if (costSavings < 0.00000001) {
                                    break;
                                } else {
                                    foundChangesInPrevLoop = true;
                                }
                                PrintCost("\t\t\t\t  LS4 Cost savings = " + costSavings, currCidToWidMap);
                                ////PrintCost("\t\t" + bestCostSoFarAcrossRestarts + "--", currCidToWidMap);
                            }
                        }
                    }
                    #endregion


                    #region LS5
                    Console.WriteLine("\t\t\t\t\t **** LS5");
                    currTrial = 0;
                    while (currTrial++ < maxTrial5) {

                        //if (currTrial % 1 == 0) {
                        //    currBestCostSoFar = GetAssignmentCost(currCidToWidMap);
                        //    Console.Write("\n\t\t\t LS3 : Trial " + currTrial + " : " + currBestCostSoFar);
                        //}

                        int c1CidToChange, c2CidToChange, c3CidToChange;
                        int w1WidToChangeTo, w2WidToChangeTo, w3WidToChangeTo;
                        FindBetterNeighbour5(currCidToWidMap, widTocidCount, assignedWarehouses, remainingCapacityFor, out c1CidToChange, out c2CidToChange, out c3CidToChange, out w1WidToChangeTo, out w2WidToChangeTo, out w3WidToChangeTo);
                        if (c1CidToChange == -1 || c2CidToChange == -1 || c3CidToChange == -1) {
                            break;
                        } else {

                            //we've found a better neighbour, rearrange the assignment list
                            int w1WidToChangeFrom = currCidToWidMap[c1CidToChange];
                            int w2WidToChangeFrom = currCidToWidMap[c2CidToChange];
                            int w3WidToChangeFrom = currCidToWidMap[c3CidToChange];
                            double costSavings = 0;

                            //unplug 1
                            costSavings += cidTowidDist[c1CidToChange, w1WidToChangeFrom];
                            remainingCapacityFor[w1WidToChangeFrom] += cidToCustomerMap[c1CidToChange].demand;
                            widTocidCount[w1WidToChangeFrom]--;

                            //unplug 2
                            costSavings += cidTowidDist[c2CidToChange, w2WidToChangeFrom];
                            remainingCapacityFor[w2WidToChangeFrom] += cidToCustomerMap[c2CidToChange].demand;
                            widTocidCount[w2WidToChangeFrom]--;

                            //unplug 3
                            costSavings += cidTowidDist[c3CidToChange, w3WidToChangeFrom];
                            remainingCapacityFor[w3WidToChangeFrom] += cidToCustomerMap[c3CidToChange].demand;
                            widTocidCount[w3WidToChangeFrom]--;

                            //plug 1 and 2 and 3
                            costSavings -= cidTowidDist[c1CidToChange, w1WidToChangeTo];
                            costSavings -= cidTowidDist[c2CidToChange, w2WidToChangeTo];
                            costSavings -= cidTowidDist[c3CidToChange, w3WidToChangeTo];
                            remainingCapacityFor[w1WidToChangeTo] -= cidToCustomerMap[c1CidToChange].demand;
                            remainingCapacityFor[w2WidToChangeTo] -= cidToCustomerMap[c2CidToChange].demand;
                            remainingCapacityFor[w3WidToChangeTo] -= cidToCustomerMap[c3CidToChange].demand;

                            widTocidCount[w1WidToChangeTo]++;
                            widTocidCount[w2WidToChangeTo]++;
                            widTocidCount[w3WidToChangeTo]++;

                            currCidToWidMap[c1CidToChange] = w1WidToChangeTo;
                            currCidToWidMap[c2CidToChange] = w2WidToChangeTo;
                            currCidToWidMap[c3CidToChange] = w3WidToChangeTo;

                            ///*now we will explore currCidToWidMap, so add this map to the list of explored paths
                            // because we do not want to explore this exact same path again. thats a waste of time.*/
                            //int hash = GetAssignmentHash(currCidToWidMap);
                            //exploredHash.Add(hash);

                            if (costSavings < 0.00000001) {
                                break;
                            } else {
                                foundChangesInPrevLoop = true;
                            }
                            PrintCost("\t\t\t\t\t  LS5 Cost savings = " + costSavings, currCidToWidMap);
                            ////PrintCost("\t\t" + bestCostSoFarAcrossRestarts + "--", currCidToWidMap);
                        }
                    }
                    #endregion

                }

                double thisBestCost = GetAssignmentCost(currCidToWidMap);
                if (thisBestCost < bestCostSoFarAcrossRestarts) {
                    bestCostSoFarAcrossRestarts = thisBestCost;
                    Array.Copy(currCidToWidMap, bestCidToWidMapAcrossRestarts, m);
                }
                Console.WriteLine("\n\t\t\t\t\t\t Best cost after this restart :" + bestCostSoFarAcrossRestarts);
            }


            //////LS2 : Take any pair c1-w1; c2-w2
            //////Try to assign then to np2 different combinations of warehouses choosing the smallest possible
            //////cost. Keep the locations not explored in a stack; so that after selecting the best path
            //////we can pop from the stack and probe the neighbourhoods for that location.


            string path = GetPath(bestCidToWidMapAcrossRestarts);
            Console.WriteLine("\n Path ::: " + path);
            Console.WriteLine(" Cost ::: " + bestCostSoFarAcrossRestarts);

            cost = GetAssignmentCost(bestCidToWidMapAcrossRestarts);
            using (StreamWriter sw = new StreamWriter("d:\\temp\\out.txt", false)) {
                sw.WriteLine(cost + " 0");
                sw.WriteLine(path);
            }
        }

        private static int GetAssignmentHash(int[] assignment) {
            int hc = assignment.Length;
            for (int i = 0; i < assignment.Length; ++i) {
                hc = unchecked(hc * 314159 + assignment[i]);
            }
            return hc;
        }

        static void AssignWarehousesRandomly(int[] cidTowidMap, int[] widTocidCount, int[] assignedWarehouses, Dictionary<int, int> remainingCapacityFor) {
            GetOriginalRemainingCapacity(remainingCapacityFor);

            for (int i = 0; i < n; i++) {
                assignedWarehouses[i] = 0;
                widTocidCount[i] = 0;
            }

            List<int> sequenceOfCust = new List<int>();
            for (int i = 0; i < m; i++) {
                sequenceOfCust.Add(i);
            }
            Shuffle(sequenceOfCust);
            
            ////List<int> sequenceOfWarehouses = new List<int>();
            ////for (int k = 0; k < n; k++) {
            ////    sequenceOfWarehouses.Add(k);
            ////}
            ////Shuffle(sequenceOfWarehouses);

            for (int i = 0; i < m; i++) { /*customers : in shuffled order*/

                List<int> sequenceOfWarehouses = new List<int>();
                for (int k = 0; k < n; k++) {
                    sequenceOfWarehouses.Add(k);
                }
                Shuffle(sequenceOfWarehouses);

                for (int widx = 0; widx < n; widx++) { /*warehouses in shuffled order*/

                    int j = sequenceOfWarehouses[widx]; /*the warehouse that we are exploring now for this customer*/
                    
                    //assign the first warehouse that satisfies this customers demand
                    if (remainingCapacityFor[j] >= cidToCustomerMap[i].demand) {
                        assignedWarehouses[j] = 1;
                        widTocidCount[j]++;
                        cidTowidMap[i] = j;
                        remainingCapacityFor[j] -= cidToCustomerMap[i].demand;
                        break;
                    }
                }
            }
        }

        static void Shuffle(IList<int> list) {
            Random rnd = new Random();
            for (var i = 0; i < list.Count; i++)
                Swap(list, i, rnd.Next(i, list.Count));
        }

        static void Swap(IList<int> list, int i, int j) {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        private static void PrintCost(string prefix, int[] cidTowidMap, out double cost) {
            cost = 0;
            if (cidTowidMap != null) {
                cost = GetAssignmentCost(cidTowidMap);
                Console.WriteLine(prefix + " " + cost);
            }
        }

        private static void PrintCost(string prefix, int[] cidTowidMap) {
            double cost;
            PrintCost(prefix, cidTowidMap, out cost);
        }

        private static string GetPath(int[] cidTowidMap) {
            string pathString = "";
            for (int i = 0; i < cidTowidMap.Length; i++) {
                pathString += cidTowidMap[i] + " ";
            }
            return pathString;
        }

        #region LS1
        private class LS1Swap {
            public int ci;
            public int wk;
        }
        //LocalSearch
        //LS1 : Take any customer ci : wj
        //iso wj, search for wk, that has a lower cost
        //after assigning a suitable wk, if wj has no customers, then remove the setup cost for wj
        private static void FindBetterNeighbour1(
            int[] cidTowidMap, 
            int[] widTocidCount, 
            int[] assignedWarehouses, 
            Dictionary<int, int> remainingCapacityFor,
            out int cidToChange,
            out int widToChangeTo,
            Stack<LS1Swap> swapList
        ) {

            cidToChange = -1;
            widToChangeTo = -1;

            //todo : sort remainingCapacityFor in sorted descending order of remaining capacity. check if this works?
            int[] remainingCapacityIndices = remainingCapacityFor.OrderByDescending(kvp => kvp.Value).Select(x => x.Key).ToArray();

            double bestCostFoundForAllCustSoFar = double.MaxValue;

            List<int> lst = new List<int>();
            for (int z = 0; z < m; z++) {
                lst.Add(z);
            }
            Shuffle(lst);

            bool onlyTheBest = new Random().Next(4) == 0 ? true : false;

            for (int _i = 0; _i < m; _i++) {
                int i = lst[_i];
                //try to remove customer i from its warehouse and find another suitable warehouse where the capacity constraints are satisfied and also the total cost is minimized
                int widForthisCust = cidTowidMap[i];
                double thisCidsOrigDist = cidTowidDist[i, widForthisCust];
                double thisCidOrigSetupCost = widTocidCount[widForthisCust] > 1 ? 0 : widToWarehouseMap[widForthisCust].setupCost;
                int thisCustDemand = cidToCustomerMap[i].demand;
                double totalOrigCost = thisCidOrigSetupCost + thisCidsOrigDist;

                double bestCostFoundForThisCustSoFar = totalOrigCost;

                ////bool foundBetterAssignment = false;
                int newWid = -1;

                for (int j = 0; j < n; j++) { /*j represents the new warehouse that we will try to assign to customer i*/
                    if (j != widForthisCust && remainingCapacityFor[j] >= thisCustDemand) {
                        double newSetUpCost = (1 - assignedWarehouses[j]) * widToWarehouseMap[j].setupCost;
                        double newTravelDist = cidTowidDist[i, j];

                        if (newSetUpCost + newTravelDist < bestCostFoundForThisCustSoFar) {

                            if (!onlyTheBest) {
                                LS1Swap swap = new LS1Swap() {
                                    ci = i, wk = j
                                };
                                swapList.Push(swap);
                            }

                            /*only if this path has already not been explored, proceed to explore this path which has better cost.
                             todo : we could also try to have this check inside the bestcost check instead of having it here.*/
                            if (!pruningEnabled || !AlreadyExplored(cidTowidMap, i, j)) {

                                //we found a better assignment. break on finding first such assignment. todo : consider all other assignments for this customer also?
                                ////foundBetterAssignment = true;
                                newWid = j;
                                bestCostFoundForThisCustSoFar = newSetUpCost + newTravelDist;
                                //break;

                                if (bestCostFoundForThisCustSoFar < bestCostFoundForAllCustSoFar) {
                                    bestCostFoundForAllCustSoFar = bestCostFoundForThisCustSoFar;

                                    cidToChange = i;
                                    widToChangeTo = newWid;

                                    if (onlyTheBest) {
                                        LS1Swap swapBest = new LS1Swap() {
                                            ci = i, wk = j
                                        };
                                        swapList.Clear();
                                        swapList.Push(swapBest);
                                    }
                                }
                            } else {
                                linearPruneCount++;
                            }
                        }
                    }
                }

                ////if (foundBetterAssignment) { /*todo : we break on first finding for first cid, explore all other cids also*/
                ////    cidToChange = i;
                ////    widToChangeTo = newWid;
                ////    break;
                ////}
            }
        }
        #endregion

        #region LS_Facility_Flip-flop

        private static void FindBetterNeighbourFacilityFlipFlop(
            ref int[] cidTowidMap,
            ref int[] widTocidCount,
            ref int[] assignedWarehouses,
            ref Dictionary<int, int> remainingCapacityFor,
            out bool success
        ) {
            success = false;
            Dictionary<int, List<int>> widToCidsAssigned = new Dictionary<int,List<int>>();
            for (int i = 0; i < m; i++) {
                int w = cidTowidMap[i];
                if (!widToCidsAssigned.ContainsKey(w)) {
                    widToCidsAssigned[w] = new List<int>();
                }
                widToCidsAssigned[w].Add(i);
            }

            for (int fopen = 0; fopen < n; fopen++) { /*facility to open*/

                if (assignedWarehouses[fopen] == 1) { /*we can only open a closed warehouse*/
                    continue;
                }

                for (int fclose = 0; fclose < n; fclose++) { /*facility to close*/

                    if (fopen == fclose || assignedWarehouses[fclose] == 0) { /*we cannot close an already closed warehouse*/
                        continue;
                    }

                    var clonedRemainingCapacityFor = new Dictionary<int, int>();
                    foreach (var kvp in remainingCapacityFor) {
                        clonedRemainingCapacityFor[kvp.Key] = kvp.Value;
                    }

                    //OPEN A NEW FACILITY
                    //assign as many customers as fopen allows in increasing order of d(c,fopen) : if d(c,fopen) < existing_d_for_c
                    var sortedCustomerDistances = widToCustomerDistMap[fopen];

                    HashSet<int> cidsToMoveToOpen = new HashSet<int>();
                    double distSavings_o = 0;
                    double setUpSavings_o = 0;
                    Dictionary<int, int> widCustReduction = new Dictionary<int,int>(); /*from each wid some cids can be removed and this data str. codifies that*/

                    bool anyAssignedTofopen = false;

                    for (int i = 0; i < sortedCustomerDistances.Count; i++) {
                        int cid = sortedCustomerDistances[i].cid;
                        if (clonedRemainingCapacityFor[fopen] >= cidToCustomerMap[cid].demand) {
                            int origWid = cidTowidMap[cid];
                            double savings = cidTowidDist[cid, origWid] - cidTowidDist[cid, fopen];
                            if (savings > 0.000001) { /*we found a shorter dist. if cid moves to fopen*/

                                anyAssignedTofopen = true;

                                clonedRemainingCapacityFor[fopen] -= cidToCustomerMap[cid].demand;
                                clonedRemainingCapacityFor[origWid] += cidToCustomerMap[cid].demand;

                                cidsToMoveToOpen.Add(cid);
                                if(!widCustReduction.ContainsKey(origWid)) {
                                    widCustReduction[origWid] = 1;
                                } else {
                                    widCustReduction[origWid]++;
                                }
                                distSavings_o += savings;

                                if (clonedRemainingCapacityFor[fopen] < 500) { //todo : the avg demand for a ls_500_7 problem value is hardcoded
                                    break;
                                }
                            }
                        }
                    }

                    if (anyAssignedTofopen) {
                        setUpSavings_o = -widToWarehouseMap[fopen].setupCost;
                    }

                    //CLOSE AN EXISTING FACILITY
                    double distSavings_c = 0;
                    double setUpSavings_c = 0;
                    
                    Dictionary<int, int> shiftedCidToWidMapFromClose = new Dictionary<int, int>();
                    var cidsToShiftFromfclose = widToCidsAssigned[fclose];
                    int shifted = 0;
                    foreach (var c in cidsToShiftFromfclose) {
                        var wareHouseDists = cidToWarehouseDistMap[c];
                        for (int k = 0; k < wareHouseDists.Count; k++) {
                            int w1 = wareHouseDists[k].wid;
                            if ((fclose != w1) && (assignedWarehouses[w1] == 1 || fopen == w1) && (clonedRemainingCapacityFor[w1] >= cidToCustomerMap[c].demand)) {
                                clonedRemainingCapacityFor[w1] -= cidToCustomerMap[c].demand;
                                shiftedCidToWidMapFromClose[c] = w1;
                                distSavings_c += cidTowidDist[c, fclose] - cidTowidDist[c, w1];
                                shifted++;

                                if (!widCustReduction.ContainsKey(w1)) {
                                    widCustReduction[w1] = -1;
                                } else {
                                    widCustReduction[w1]--; /*not a reduction of customers to w1, but rather a addition, i.e "-1" customers removed*/
                                }

                                break;
                            }
                        }
                    }

                    if (shifted == cidsToShiftFromfclose.Count) { /*all cids have been shifted, so this setup cost is saved*/
                        setUpSavings_c = widToWarehouseMap[fclose].setupCost;
                    }

                    double setupSavings_open_close = 0;
                    //if all customers of any given facility are assigned then close the facility
                    HashSet<int> facilitiesToClose = new HashSet<int>();
                    foreach (var kvp in widCustReduction) {
                        if (widTocidCount[kvp.Key] == kvp.Value) {
                            facilitiesToClose.Add(kvp.Key);
                            setupSavings_open_close += widToWarehouseMap[kvp.Key].setupCost;
                        }
                    }

                    if (setUpSavings_o + setUpSavings_c + setupSavings_open_close + distSavings_o + distSavings_c > 0.000001) {
                        /*by opening 1 facility and closing another facility, we have indeed found a better cost assignment*/
                        foreach (int cid in cidsToMoveToOpen) {
                            cidTowidMap[cid] = fopen;
                        }
                        foreach (var kvp in shiftedCidToWidMapFromClose) {
                            cidTowidMap[kvp.Key] = kvp.Value;
                        }
                        foreach (var kvp in widCustReduction) {
                            widTocidCount[kvp.Key] -= kvp.Value;
                        }
                        if (anyAssignedTofopen) {
                            assignedWarehouses[fopen] = 1;
                        }
                        if (shifted == cidsToShiftFromfclose.Count) {
                            assignedWarehouses[fclose] = 0;
                        }
                        foreach (var f_c in facilitiesToClose) {
                            assignedWarehouses[f_c] = 0;
                        }
                        remainingCapacityFor = clonedRemainingCapacityFor;
                        success = true;
                        goto end;
                    }
                }
            }
        end:
            return;
        }

        #endregion

        #region LS2
        private class LS2Swap {
            public int c1;
            public int c2;
            public int wk;
        }

        //LS2 : Take any pair c1-w1; c2-w2
        //Try to assign then to n different warehouses choosing the smallest possible
        //cost. Keep the locations not explored in a stack; so that after selecting the best path
        //we can pop from the stack and probe the neighbourhoods for that location.
        private static void FindBetterNeighbour2(
            int[] cidTowidMap,
            int[] widTocidCount,
            int[] assignedWarehouses,
            Dictionary<int, int> remainingCapacityFor,
            out int c1CidToChange,
            out int c2CidToChange,
            out int widToChangeTo,
            List<LS2Swap> ls2List
        ) {
            double bestCostFoundForAllCustSoFar = double.MaxValue;
            c1CidToChange = -1;
            c2CidToChange = -1;
            widToChangeTo = -1;

            //take any 2 customers
            int w1;
            int w2;
            for (int c1 = 0; c1 < m - 1; c1++) { /*cust1*/
                w1 = cidTowidMap[c1];
                for (int c2 = c1 + 1; c2 < m; c2++) { /*cust2*/
                    //first try to move c1 and c2 to a single new warehouse w_k such that the total cost is minimized, w_k != w1 or w2
                    w2 = cidTowidMap[c2];

                    double c1ThisCidsOrigDist = cidTowidDist[c1, w1];
                    double c1ThisCidOrigSetupCost = widTocidCount[w1] > 1 ? 0 : widToWarehouseMap[w1].setupCost;
                    int c1ThisCustDemand = cidToCustomerMap[c1].demand;
                    double c1TotalOrigCost = c1ThisCidOrigSetupCost + c1ThisCidsOrigDist;

                    double c2ThisCidsOrigDist = cidTowidDist[c2, w2];
                    double c2ThisCidOrigSetupCost = widTocidCount[w2] > 1 ? 0 : widToWarehouseMap[w2].setupCost;
                    int c2ThisCustDemand = cidToCustomerMap[c2].demand;
                    double c2TotalOrigCost = c2ThisCidOrigSetupCost + c2ThisCidsOrigDist;

                    double bestCostFoundForThisCustSoFar = c1TotalOrigCost + c2TotalOrigCost;

                    ////bool foundBetterAssignment = false;
                    int newWid = -1;

                    for (int j = 0; j < n; j++) { /*j represents the new warehouses that we will try to assign to customer c1 and c2*/
                        if (j != w1 && j != w2 && remainingCapacityFor[j] >= c1ThisCustDemand + c2ThisCustDemand) {
                            double newSetUpCost = (1 - assignedWarehouses[j]) * widToWarehouseMap[j].setupCost;
                            double c1NewTravelDist = cidTowidDist[c1, j];
                            double c2NewTravelDist = cidTowidDist[c2, j];

                            if (newSetUpCost + c1NewTravelDist + c2NewTravelDist < bestCostFoundForThisCustSoFar) {

                                LS2Swap swapItem = new LS2Swap() {
                                    c1 = c1, c2 = c2, wk = j
                                };
                                ls2List.Add(swapItem);
                                goto end;

                                if (!pruningEnabled || !AlreadyExplored(cidTowidMap, c1, c2, j)) {

                                    //we found a better assignment. break on finding first such assignment. todo : consider all other assignments for this customer also?
                                    ////foundBetterAssignment = true;
                                    newWid = j;
                                    bestCostFoundForThisCustSoFar = newSetUpCost + c1NewTravelDist + c2NewTravelDist;
                                    //break;

                                    if (bestCostFoundForThisCustSoFar < bestCostFoundForAllCustSoFar) {
                                        bestCostFoundForAllCustSoFar = bestCostFoundForThisCustSoFar;

                                        c1CidToChange = c1;
                                        c2CidToChange = c2;
                                        widToChangeTo = newWid;
                                    }
                                } else {
                                    linearPruneCount++;
                                }
                            }
                        }
                    }
                }
            }
        end:
            return;
        }
        #endregion

        #region LS2
        //LS2 : Take any pair c1-w1; c2-w2
        //Try to assign then to n different warehouses choosing the smallest possible
        //cost. Keep the locations not explored in a stack; so that after selecting the best path
        //we can pop from the stack and probe the neighbourhoods for that location.
        private static void FindBetterNeighbour22(
            int[] cidTowidMap,
            int[] widTocidCount,
            int[] assignedWarehouses,
            Dictionary<int, int> remainingCapacityFor,
            out int c1CidToChange,
            out int c2CidToChange,
            out int widToChangeTo,
            List<LS2Swap> ls2List
        ) {
            double bestCostFoundForAllCustSoFar = double.MaxValue;
            c1CidToChange = -1;
            c2CidToChange = -1;
            widToChangeTo = -1;

            //take any 2 customers
            int w1;
            int w2;

            List<int> orig = new List<int>();
            for (int i = 0; i < m ; i++) {
                orig.Add(i);
            }
            Shuffle(orig);

            int cap = m / 100;
            for (int _c1 = 0; _c1 < cap; _c1++) { /*cust1*/
                int c1 = orig[_c1];
                w1 = cidTowidMap[c1];
                for (int c2 = 0; c2 < m; c2++) { /*cust2*/

                    if (c1 == c2) {
                        continue;
                    }

                    //first try to move c1 and c2 to a single new warehouse w_k such that the total cost is minimized, w_k != w1 or w2
                    w2 = cidTowidMap[c2];

                    double c1ThisCidsOrigDist = cidTowidDist[c1, w1];
                    double c1ThisCidOrigSetupCost = widTocidCount[w1] > 1 ? 0 : widToWarehouseMap[w1].setupCost;
                    int c1ThisCustDemand = cidToCustomerMap[c1].demand;
                    double c1TotalOrigCost = c1ThisCidOrigSetupCost + c1ThisCidsOrigDist;

                    double c2ThisCidsOrigDist = cidTowidDist[c2, w2];
                    double c2ThisCidOrigSetupCost = widTocidCount[w2] > 1 ? 0 : widToWarehouseMap[w2].setupCost;
                    int c2ThisCustDemand = cidToCustomerMap[c2].demand;
                    double c2TotalOrigCost = c2ThisCidOrigSetupCost + c2ThisCidsOrigDist;

                    double bestCostFoundForThisCustSoFar = c1TotalOrigCost + c2TotalOrigCost;

                    ////bool foundBetterAssignment = false;
                    int newWid = -1;

                    for (int j = 0; j < n; j++) { /*j represents the new warehouses that we will try to assign to customer c1 and c2*/
                        if (j != w1 && j != w2 && remainingCapacityFor[j] >= c1ThisCustDemand + c2ThisCustDemand) {
                            double newSetUpCost = (1 - assignedWarehouses[j]) * widToWarehouseMap[j].setupCost;
                            double c1NewTravelDist = cidTowidDist[c1, j];
                            double c2NewTravelDist = cidTowidDist[c2, j];

                            if (newSetUpCost + c1NewTravelDist + c2NewTravelDist < bestCostFoundForThisCustSoFar) {

                                LS2Swap swapItem = new LS2Swap() {
                                    c1 = c1, c2 = c2, wk = j
                                };
                                ls2List.Add(swapItem);
                                goto end;

                                if (!pruningEnabled || !AlreadyExplored(cidTowidMap, c1, c2, j)) {

                                    //we found a better assignment. break on finding first such assignment. todo : consider all other assignments for this customer also?
                                    ////foundBetterAssignment = true;
                                    newWid = j;
                                    bestCostFoundForThisCustSoFar = newSetUpCost + c1NewTravelDist + c2NewTravelDist;
                                    //break;

                                    if (bestCostFoundForThisCustSoFar < bestCostFoundForAllCustSoFar) {
                                        bestCostFoundForAllCustSoFar = bestCostFoundForThisCustSoFar;

                                        c1CidToChange = c1;
                                        c2CidToChange = c2;
                                        widToChangeTo = newWid;
                                    }
                                } else {
                                    linearPruneCount++;
                                }
                            }
                        }
                    }
                }
            }
        end:
            return;
        }
        #endregion

        #region LS21
        //LS2 : Take any pair c1-w1; c2-w2
        //Try to assign then to n different warehouses choosing the smallest possible
        //cost. Keep the locations not explored in a stack; so that after selecting the best path
        //we can pop from the stack and probe the neighbourhoods for that location.
        private static void FindBetterNeighbour21(
            int[] cidTowidMap,
            int[] widTocidCount,
            int[] assignedWarehouses,
            Dictionary<int, int> remainingCapacityFor,
            out int c1CidToChange,
            out int c2CidToChange,
            out int widToChangeTo,
            List<LS2Swap> ls2List
        ) {
            ls2List.Clear();
            c1CidToChange = -1;
            c2CidToChange = -1;
            widToChangeTo = -1;

            openWidToCidsMap.Clear();
            for (int cid = 0; cid < m; cid++) {
                if (!openWidToCidsMap.ContainsKey(cidTowidMap[cid])) {
                    openWidToCidsMap[cidTowidMap[cid]] = new HashSet<int>();
                }
                openWidToCidsMap[cidTowidMap[cid]].Add(cid);
            }

            var widsHavingCids = openWidToCidsMap.Keys.ToList();
            int numOfWids = widsHavingCids.Count;

            for (int _w1 = 0; _w1 < numOfWids - 1; _w1++) {
                for (int _w2 = _w1 + 1; _w2 < numOfWids; _w2++) {

                    int w1 = widsHavingCids[_w1];
                    int w2 = widsHavingCids[_w2];
                    var w1_cList = openWidToCidsMap[w1];
                    var w2_cList = openWidToCidsMap[w2];

                    foreach (var c1 in w1_cList) {
                        foreach (var c2 in w2_cList) {

                            //required capacity
                            int reqCap = cidToCustomerMap[c1].demand + cidToCustomerMap[c2].demand;

                            //old_distance
                            double oldDist = cidTowidDist[c1, w1] + cidTowidDist[c2, w2];

                            double setupOverhead = 0;

                            for (int i = 0; i < n; i++) {
                                if (i != w1 && i != w2) {
                                    if ((remainingCapacityFor[i] >= reqCap)) { /*and consider only warehouses that can satisfy the required capacity*/
                                        /*is setup overhead is -ve then it is good for us, because we save that much amount of cost*/
                                        if (openWidToCidsMap[w1].Count == 1) {
                                            setupOverhead -= widToWarehouseMap[w1].setupCost;
                                        }
                                        if (openWidToCidsMap[w2].Count == 1) {
                                            setupOverhead -= widToWarehouseMap[w1].setupCost;
                                        }
                                        if (!openWidToCidsMap.ContainsKey(i)) {
                                            setupOverhead += widToWarehouseMap[i].setupCost;
                                        }

                                        if (oldDist - cidTowidDist[c1, i] - cidTowidDist[c2, i] - setupOverhead > 0.00001) /*and consider only those warehouses which offer a better cost n0w*/ {
                                            ls2List.Add(new LS2Swap() { c1 = c1, c2 = c2, wk = i });
                                            goto end; /*break on first instance, also consider the option of selecting a single good option each for every c1,c2*/
                                        }
                                    }
                                }
                            }

                            #region unused
                            ////var validWids = remainingCapacityFor.Where(x =>
                            ////    ////(openWidToCidsMap.ContainsKey(x.Value)) &&  /*consider only already open warehouses*/
                            ////    (x.Key != w1 && x.Key != w2) && /*dont move to w1 or w2*/
                            ////    (x.Value >= reqCap) &&  /*and consider only warehouses that can satisfy the required capacity*/
                            ////    (oldDist - cidTowidDist[c1, x.Key] + cidTowidDist[c2, x.Key] > 0.00001) /*and consider only those warehouses which offer a better cost n0w*/
                            ////).Select(y => y.Key).ToList();

                            ////var validWids = openWidToCidsMap.Where(x => /*consider only already open warehouses*/
                            ////    (x.Key != w1 && x.Key != w2) && /*dont move to w1 or w2*/
                            ////    (remainingCapacityFor[x.Key] >= reqCap) &&  /*and consider only warehouses that can satisfy the required capacity*/
                            ////    (oldDist > cidTowidDist[c1, x.Key] + cidTowidDist[c2, x.Key]) /*and consider only those warehouses which offer a better cost n0w*/
                            ////).Select(y => y.Key).ToList();
                            #endregion

                            
                        }
                    }
                }
            }
        end:
            return;
        }
        #endregion

        #region LS3
        //LS2 : Take any pair c1-w1; c2-w2
        //Try to assign then to np2 different combinations of warehouses choosing the smallest possible
        //cost. Keep the locations not explored in a stack; so that after selecting the best path
        //we can pop from the stack and probe the neighbourhoods for that location.
        private static void FindBetterNeighbour3(
            int[] cidTowidMap,
            int[] widTocidCount,
            int[] assignedWarehouses,
            Dictionary<int, int> remainingCapacityFor,
            out int c1CidToChange,
            out int c2CidToChange,
            out int w1WidToChangeTo,
            out int w2WidToChangeTo
        ) {
            double bestCostFoundForAllCustSoFar = double.MaxValue;
            c1CidToChange = -1;
            c2CidToChange = -1;
            w1WidToChangeTo = -1;
            w2WidToChangeTo = -1;

            //take any 2 customers
            int w1;
            int w2;
            for (int c1 = 0; c1 < m - 1; c1++) { /*cust1*/
                w1 = cidTowidMap[c1];
                for (int c2 = c1 + 1; c2 < m; c2++) { /*cust2*/
                    //first try to move c1 and c2 to w_k1 and w_k2 such that the total cost is minimized, w_k1 != w_k2
                    w2 = cidTowidMap[c2];

                    double c1ThisCidsOrigDist = cidTowidDist[c1, w1];
                    double c1ThisCidOrigSetupCost = widTocidCount[w1] > 1 ? 0 : widToWarehouseMap[w1].setupCost;
                    int c1ThisCustDemand = cidToCustomerMap[c1].demand;
                    double c1TotalOrigCost = c1ThisCidOrigSetupCost + c1ThisCidsOrigDist;

                    double c2ThisCidsOrigDist = cidTowidDist[c2, w2];
                    double c2ThisCidOrigSetupCost = widTocidCount[w2] > 1 ? 0 : widToWarehouseMap[w2].setupCost;
                    int c2ThisCustDemand = cidToCustomerMap[c2].demand;
                    double c2TotalOrigCost = c2ThisCidOrigSetupCost + c2ThisCidsOrigDist;

                    double bestCostFoundForThisCustSoFar = c1TotalOrigCost + c2TotalOrigCost;

                    ////bool foundBetterAssignment = false;
                    int w1NewWid = -1;
                    int w2NewWid = -1;

                    for (int j1 = 0; j1 < n-1; j1++) { /*j1 represents the first of new warehouses that we will try to assign to customer c1 and c2*/

                        for (int j2 = j1+1; j2 < n; j2++) { /*j2 represents the second of new warehouses that we will try to assign to customer c1 and c2*/

                            if (!((j1 == w1 && j2 == w2) || (j1 == w2 && j2 == w2))) { /*do not explore the exact same warehouses w1 and w2 again for c1 and c2*/

                                bool constraintSafisfied = false;
                                int w_k1 = -1, w_k2 = -1;
                                if (remainingCapacityFor[j1] >= c1ThisCustDemand && remainingCapacityFor[j2] >= c2ThisCustDemand) {
                                    constraintSafisfied = true;
                                    w_k1 = j1;
                                    w_k2 = j2;
                                } else if (remainingCapacityFor[j2] >= c1ThisCustDemand && remainingCapacityFor[j1] >= c2ThisCustDemand) {
                                    constraintSafisfied = true;
                                    w_k1 = j2;
                                    w_k2 = j1;
                                } 

                                if (constraintSafisfied && (remainingCapacityFor[w_k1] + remainingCapacityFor[w_k2] >= c1ThisCustDemand + c2ThisCustDemand)) {
                                    double newSetUpCost1 = (1 - assignedWarehouses[w_k1]) * widToWarehouseMap[w_k1].setupCost;
                                    double newSetUpCost2 = (1 - assignedWarehouses[w_k2]) * widToWarehouseMap[w_k2].setupCost;
                                    double c1NewTravelDist = cidTowidDist[c1, w_k1];
                                    double c2NewTravelDist = cidTowidDist[c2, w_k2];

                                    if (newSetUpCost1 + newSetUpCost2 + c1NewTravelDist + c2NewTravelDist < bestCostFoundForThisCustSoFar) {

                                        //we found a better assignment. break on finding first such assignment. todo : consider all other assignments for this customer also?
                                        ////foundBetterAssignment = true;
                                        w1NewWid = w_k1;
                                        w2NewWid = w_k2;
                                        bestCostFoundForThisCustSoFar = newSetUpCost1 + newSetUpCost2 + c1NewTravelDist + c2NewTravelDist;
                                        //break;

                                        if (bestCostFoundForThisCustSoFar < bestCostFoundForAllCustSoFar) {
                                            bestCostFoundForAllCustSoFar = bestCostFoundForThisCustSoFar;

                                            c1CidToChange = c1;
                                            c2CidToChange = c2;
                                            w1WidToChangeTo = w1NewWid;
                                            w2WidToChangeTo = w2NewWid;
                                            goto end;
                                        }
                                    }
                                }
                            }
                        }

                        ////if (foundBetterAssignment) { /*todo : we break on first finding for first cid, explore all other cids also*/
                        ////    cidToChange = i;
                        ////    widToChangeTo = newWid;
                        ////    break;
                        ////}
                    }
                }
            }
            end: 
            return;
        }
        #endregion

        #region LS4
        //LS2 : Take any pair c1-w1; c2-w2
        //Try to swap them choosing the smallest possible cost. 
        private static void FindBetterNeighbour4(
            int[] cidTowidMap,
            int[] widTocidCount,
            int[] assignedWarehouses,
            Dictionary<int, int> remainingCapacityFor,
            out int c1CidToChange,
            out int c2CidToChange,
            out int w1WidToChangeTo,
            out int w2WidToChangeTo
        ) {
            double bestCostFoundForAllCustSoFar = GetAssignmentCost(cidTowidMap);
            double bestCostDiff = 0;
            c1CidToChange = -1;
            c2CidToChange = -1;
            w1WidToChangeTo = -1;
            w2WidToChangeTo = -1;

            //take any 2 customers
            int w1;
            int w2;
            for (int c1 = 0; c1 < m - 1; c1++) { /*cust1*/
                w1 = cidTowidMap[c1];
                for (int c2 = c1 + 1; c2 < m; c2++) { /*cust2*/
                    //swap warehouses for c1 and c2 s.t the total cost is minimized.
                    w2 = cidTowidMap[c2];

                    int c1Demand = cidToCustomerMap[c1].demand;
                    int c2Demand = cidToCustomerMap[c2].demand;

                    if (c1Demand != c2Demand) {
                        bool constraintSatisfied = false;
                        int deltaDemand = Math.Abs(c1Demand - c2Demand);
                        if (c1Demand > c2Demand) { /*c1 will come to w2, so check if w2 has enough space*/
                            if (remainingCapacityFor[w2] >= deltaDemand) {
                                constraintSatisfied = true;
                            }
                        } else { /*c2 will come to w1, so check if w1 has enough space*/
                            if (remainingCapacityFor[w1] >= deltaDemand) {
                                constraintSatisfied = true;
                            }
                        }

                        if (constraintSatisfied) {
                            double thisCostDiff = cidTowidDist[c1, w1] + cidTowidDist[c2, w2] - cidTowidDist[c1, w2] - cidTowidDist[c2, w1];
                            if (thisCostDiff > bestCostDiff) {
                                bestCostDiff = thisCostDiff;
                                c1CidToChange = c1;
                                c2CidToChange = c2;
                                w1WidToChangeTo = w2;
                                w2WidToChangeTo = w1;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        static int prev_ls5_c1 = 0;

        #region LS5
        //LS2 : Take any triplet c1-w1; c2-w2; c3-w3
        //Try to swap them choosing the smallest possible cost. 
        private static void FindBetterNeighbour5(
            int[] cidTowidMap,
            int[] widTocidCount,
            int[] assignedWarehouses,
            Dictionary<int, int> remainingCapacityFor,
            out int c1CidToChange,
            out int c2CidToChange,
            out int c3CidToChange,
            out int w1WidToChangeTo,
            out int w2WidToChangeTo,
            out int w3WidToChangeTo
        ) {
            double bestCostFoundForAllCustSoFar = GetAssignmentCost(cidTowidMap);
            double bestCostDiff = 0;
            c1CidToChange = -1;
            c2CidToChange = -1;
            c3CidToChange = -1;
            w1WidToChangeTo = -1;
            w2WidToChangeTo = -1;
            w3WidToChangeTo = -1;

            //take any 2 customers
            int w1;
            int w2;
            int w3;

            int c1_i1 = prev_ls5_c1 > m - 2 ? 0 : prev_ls5_c1;
            prev_ls5_c1 = c1_i1;

            for (int c1 = c1_i1; c1 < m - 2; c1++) { /*cust1*/
                w1 = cidTowidMap[c1];
                for (int c2 = c1 + 1; c2 < m - 1; c2++) { /*cust2*/
                    w2 = cidTowidMap[c2];

                    for (int c3 = c2 + 1; c3 < m; c3++) { /*Cust3*/
                        w3 = cidTowidMap[c3];

                        int c1Demand = cidToCustomerMap[c1].demand;
                        int c2Demand = cidToCustomerMap[c2].demand;
                        int c3Demand = cidToCustomerMap[c3].demand;

                        if ((c1Demand != c2Demand) && (c2Demand != c3Demand) && (c3Demand != c1Demand)) { /*if any 2 demands are equal, then this LS becomes LS4*/

                            //c1-w1, c2-w2, c3-w3
                            //2 different combinations
                            //c1-w2, c2-w3, c3-w1
                            //c1-w3, c2-w1, c3-w2
                            //find which is cheaper than (c1-w1, c2-w2, c3-w3)
                            int comb1_w1Delta = cidToCustomerMap[c3].demand - cidToCustomerMap[c1].demand; /*this newly comes into w1*/
                            int comb1_w2Delta = cidToCustomerMap[c1].demand - cidToCustomerMap[c2].demand; /*this newly comes into w2*/
                            int comb1_w3Delta = cidToCustomerMap[c2].demand - cidToCustomerMap[c3].demand; /*this newly comes into w3*/
                            double comb1Savings = double.MinValue;
                            if (remainingCapacityFor[w1] >= comb1_w1Delta && remainingCapacityFor[w2] >= comb1_w2Delta && remainingCapacityFor[w3] >= comb1_w3Delta) {
                                /*if this is positive we have some savings*/
                                comb1Savings = 
                                    cidTowidDist[c1, w1] + 
                                    cidTowidDist[c2, w2] + 
                                    cidTowidDist[c3, w3] - 
                                    cidTowidDist[c1, w2] - 
                                    cidTowidDist[c2, w3] - 
                                    cidTowidDist[c3, w1];
                            }

                            int comb2_w1Delta = cidToCustomerMap[c2].demand - cidToCustomerMap[c1].demand; /*this newly comes into w1*/
                            int comb2_w2Delta = cidToCustomerMap[c3].demand - cidToCustomerMap[c2].demand; /*this newly comes into w2*/
                            int comb2_w3Delta = cidToCustomerMap[c1].demand - cidToCustomerMap[c3].demand; /*this newly comes into w3*/
                            double comb2Savings = double.MinValue;
                            if (remainingCapacityFor[w1] >= comb1_w1Delta && remainingCapacityFor[w2] >= comb1_w2Delta && remainingCapacityFor[w3] >= comb1_w3Delta) {
                                /*if this is positive we have some savings*/
                                comb2Savings =
                                    cidTowidDist[c1, w1] +
                                    cidTowidDist[c2, w2] +
                                    cidTowidDist[c3, w3] -
                                    cidTowidDist[c1, w3] -
                                    cidTowidDist[c2, w1] -
                                    cidTowidDist[c3, w2];
                            }

                            double thisBestCombSavings = Math.Max(comb1Savings, comb2Savings);

                            if (thisBestCombSavings > 0.000000001 && thisBestCombSavings > bestCostDiff) {/*a better solution is obtained*/
                                bestCostDiff = thisBestCombSavings;
                                c1CidToChange = c1;
                                c2CidToChange = c2;
                                c3CidToChange = c3;
                                if (comb1Savings > comb2Savings) { /*comb1 is the better of the 2*/
                                    w1WidToChangeTo = w2; /*c1 changes to this warehouse now*/
                                    w2WidToChangeTo = w3;
                                    w3WidToChangeTo = w1;
                                } else { /*comb2 is the better of the 2*/
                                    w1WidToChangeTo = w3;
                                    w2WidToChangeTo = w1;
                                    w3WidToChangeTo = w2;
                                }
                                goto end;
                            }
                        }
                    }
                }
            }
            end:
                return;
        }
        
        #endregion

        private static bool AlreadyExplored(int[] cidTowidMap, int cid, int newWid) {
            int oldWid = cidTowidMap[cid];
            cidTowidMap[cid] = newWid;
            int hash = GetAssignmentHash(cidTowidMap);
            cidTowidMap[cid] = oldWid;
            return exploredHash.Contains(hash);
        }

        private static bool AlreadyExplored(int[] cidTowidMap, int cid1, int cid2, int newWid) {
            int oldWid1 = cidTowidMap[cid1];
            int oldWid2 = cidTowidMap[cid2];
            cidTowidMap[cid1] = newWid;
            cidTowidMap[cid2] = newWid;
            int hash = GetAssignmentHash(cidTowidMap);
            cidTowidMap[cid1] = oldWid1;
            cidTowidMap[cid2] = oldWid2;
            return exploredHash.Contains(hash);
        }
    }
}
