using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GraphColoring {
    class Graph {
        public HashSet<int> GetIncidentNodes(int u) {
            return incidentNodesFor[u];
        }

        public List<int> GetAllNodeIds() {
            return incidentNodesFor.Keys.ToList();
        }

        public void AddEdge(int u, int v) {
            if (!incidentNodesFor.ContainsKey(u)) {
                incidentNodesFor.Add(u, new HashSet<int>());
                nodeOf.Add(u, new Node(u));
            }
            if (!incidentNodesFor.ContainsKey(v)) {
                incidentNodesFor.Add(v, new HashSet<int>());
                nodeOf.Add(v, new Node(v));
            }

            incidentNodesFor[u].Add(v);
            incidentNodesFor[v].Add(u);

            var nodeU = nodeOf[u];
            var nodeV = nodeOf[v];
            nodeU.AddIncidentNode(nodeV);
            nodeV.AddIncidentNode(nodeU);
        }

        public HashSet<int> GetExcludedColorsFor(int u) {
            return nodeOf[u].getExcludedColors();
        }

        public void ExcludeColorForAllIncidentNodesIfNotAlreadyColored(int node, int color) {
            foreach (var v in incidentNodesFor[node]) {
                if (!nodeOf[v].IsColored()) {
                    nodeOf[v].ExcludeColor(color);
                }
            }
        }

        public void SetColorForNode(int node, int color) {
            nodeOf[node].SetColor(color);
        }

        public int GetColorForNode(int node) {
            return nodeOf[node].GetColor();
        }

        public List<int> GetNodesSortedByConnectivity() {
            var nodeArray = nodeOf.Values.ToArray();
            Array.Sort(nodeArray, new SortByMaxIncidentNodes());
            return nodeArray.Select(x => x.GetId()).ToList();
        }

        private Dictionary<int, HashSet<int>> incidentNodesFor = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, Node> nodeOf = new Dictionary<int, Node>();

        private class Node {
            public Node(int id) {
                this.id = id;
                colored = false;
            }

            public int GetId() {
                return id;
            }

            public HashSet<int> getExcludedColors() {
                return excludedColors;
            }

            public bool IsColorExcluded(int color) {
                return excludedColors.Contains(color);
            }

            public void ExcludeColor(int color) {
                excludedColors.Add(color);
            }

            public bool IsColored() {
                return colored;
            }

            public int GetColor() {
                if (colored) {
                    return color;
                } else {
                    return -1;
                }
            }

            public void SetColor(int color) {
                this.color = color;
                colored = true;
            }

            public void AddIncidentNode(Node v) {
                incidentNodes.Add(v.GetId(), v);
            }

            public int GetNumberOfIncidentNodes() {
                return incidentNodes.Count;
            }

            private HashSet<int> excludedColors = new HashSet<int>();
            private readonly int id;
            private bool colored;
            private int color;

            private Dictionary<int, Node> incidentNodes = new Dictionary<int, Node>();
        }

        class SortByMaxIncidentNodes : IComparer<Node> {

            public int Compare(Node x, Node y) {
                int xIncidentNodes = x.GetNumberOfIncidentNodes();
                int yIncidentNodes = y.GetNumberOfIncidentNodes();
                if (xIncidentNodes > yIncidentNodes) return -1;
                else if (yIncidentNodes > xIncidentNodes) return 1;
                else return 0;
            }
        }
    }

    class Program {

        class StackEntry {
            public int Node;
            private int currColor = -1;
            public Dictionary<int, bool[]> EligibilityArrayForAdjNodes = new Dictionary<int, bool[]>();
            //private bool[] eligible = new bool[maxColor];

            //public void MarkAllEligible() {
            //    for (int i = 0; i < maxColor; i++) {
            //        eligible[i] = true;
            //    }
            //}

            public int GetCurrentColor() {
                return currColor;
            }

            public int SetNextEligibleColor() {
                int currTry = -1;
                if (currColor < maxColor) {
                    if (currColor != -1) {
                        EligibilityArrayForAdjNodes[Node][currColor] = false;
                    }
                    currTry = currColor + 1;
                    while (currTry < maxColor && !EligibilityArrayForAdjNodes[Node][currTry]) {
                        currTry++;
                    }
                }

                currColor = currTry;
                if (currTry == maxColor) {
                    return -1;
                } else {
                    return currColor;
                }
            }
        }

        static int maxColor = 10;
        static int totalNodes;
        static HashSet<int> inStack = new HashSet<int>();
        static Graph graph = null;

        static void Main(string[] args) {
            string fName = @"D:\san\Algo\courses\Coursera-DsicreteOptimization PascalVanHentenryck\Lectures\_Assignments\coloring\coloring\data\gc_20_9";

            bool debug = true;

            if (args.Length > 0) {
                debug = false;
                fName = args[0];
                //Console.WriteLine("Input file : " + fName);
            }

            if (debug) {
                Console.WriteLine("File = " + fName);
                Console.WriteLine("Max Color = " + maxColor);
            }

            graph = ReadGraphFromFile(fName);

            Stack<StackEntry> stack = new Stack<StackEntry>();
            List<int> nodesToProcess = graph.GetNodesSortedByConnectivity();
            totalNodes = nodesToProcess.Count;

            int curr = 0;
            
            inStack.Add(nodesToProcess[curr]);

            StackEntry first = new StackEntry() {
                Node = nodesToProcess[curr],
                EligibilityArrayForAdjNodes = GetInitialEligibility()
            };
            //first.MarkAllEligible();
            stack.Push(first);

            bool solutionFound = false;

            while (stack.Count > 0) {

                

                StackEntry top = stack.Peek();
                int nextEligibleColor = top.SetNextEligibleColor();

                if (nextEligibleColor == -1) {
                    //we cannot continue search in this tree path. so pop, restore state and search some other path

                    StackEntry popped = stack.Pop();
                    inStack.Remove(popped.Node);
                    curr--;

                    //this automatically restores the state


                } else {

                    if (debug && stack.Count < 20) {
                        Console.Write("Setting Color = " + nextEligibleColor);
                        PrintStackForDiagnostics(stack);
                    }

                    //check if all nodes have been colored. 
                    //if so, report result
                    if (stack.Count == nodesToProcess.Count) {
                        if (debug) {
                            Console.WriteLine("Solution found!!");
                        }
                        ReportResult(stack);
                        solutionFound = true;
                        break;
                    }

                    //if not propage color to adj nodes and push next
                    var incidentNodes = graph.GetIncidentNodes(top.Node);
                    Dictionary<int, bool[]> eligibilityArrayForAdjNodes = GetAdjEligibilityArrayAfterPropagatingCurrColor(top);

                    curr++;
                    StackEntry next = new StackEntry() {
                        EligibilityArrayForAdjNodes = eligibilityArrayForAdjNodes,
                        Node = nodesToProcess[curr]
                    };
                    stack.Push(next);
                    inStack.Add(nodesToProcess[curr]);
                }
            }

            if (debug && !solutionFound) {
                Console.WriteLine("oops!! no solution could be found with the given constraints.");
            }
        }

        private static void PrintStackForDiagnostics(Stack<StackEntry> stack) {
            StackEntry[] clone = new StackEntry[stack.Count];
            stack.CopyTo(clone, 0);

            Console.Write("Exploring path..");
            for (int i = stack.Count-1; i >= 0; i--) {
                Console.Write(clone[i].Node + " (" + clone[i].GetCurrentColor() + ")" + " ");
            }
            Console.WriteLine();
        }

        private static void ReportResult(Stack<StackEntry> stack) {
            Dictionary<int, int> temp = new Dictionary<int, int>();
            while (stack.Count > 0) {
                var pop = stack.Pop();
                temp.Add(pop.Node, pop.GetCurrentColor());
            }

            Console.WriteLine(maxColor + " 0");

            for (int i = 0; i < totalNodes; i++) {
                Console.Write(temp[i] + " ");
            }

            Console.WriteLine();
        }

        private static Dictionary<int, bool[]> GetAdjEligibilityArrayAfterPropagatingCurrColor(StackEntry top) {
            Dictionary<int, bool[]> eligibilityArrayFor = new Dictionary<int, bool[]>();

            foreach (var kvp in top.EligibilityArrayForAdjNodes) {
                bool[] eligible = new bool[maxColor];
                Array.Copy(kvp.Value, eligible, maxColor);

                int takenColor = top.GetCurrentColor();
                if (graph.GetIncidentNodes(top.Node).Contains(kvp.Key)) {
                    eligible[takenColor] = false;
                }

                eligibilityArrayFor[kvp.Key] = eligible;
            }
            return eligibilityArrayFor;
        }

        private static Dictionary<int, bool[]> GetInitialEligibility() {
            Dictionary<int, bool[]> eligibilityArrayFor = new Dictionary<int, bool[]>();
            for (int i = 0; i < totalNodes; i++) {
                bool[] eligible = new bool[maxColor];
                for (int j = 0; j < maxColor; j++) {
                    eligible[j] = true;
                }
                eligibilityArrayFor[i] = eligible;
            }
            return eligibilityArrayFor;
        }

        static Graph ReadGraphFromFile(string fName) {
            Graph g = new Graph();

            var lines = File.ReadAllLines(fName);
            var header = lines[0].Split(' ');

            var numNodes = int.Parse(header[0]);
            var numEdges = int.Parse(header[1]);

            for (int i = 1; i <= numEdges; i++) {
                var uv = lines[i].Split(' ');
                var u = int.Parse(uv[0]);
                var v = int.Parse(uv[1]);

                g.AddEdge(u, v);
            }

            return g;
        }
    }
}
