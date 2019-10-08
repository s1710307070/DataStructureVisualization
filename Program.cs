using System;
using System.Collections.Generic;


namespace DataStructureVisualization
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            //binary tree
            {
                BinaryTree testTree = new BinaryTree();
                Random rnd = new Random();
                for (int i = 0; i < 5; i++) testTree.Insert(rnd.Next(1, 10000));
                //testTree.DisplayTree();

                List<String> testList1 = new List<String>() {"Data"};
                DataStructureVisualizer.Visualize(testTree, testList1);
                //DataStructureVisualizer.Visualize(testTree, testList1);
            }

            
            //nodes with data members and left/right references
            {
                Node testNode1 = new Node(1);
                Node testNode2 = new Node(2);
                testNode1.Left = testNode1;
                testNode2.Right = testNode1;
                //test1.visibleNames = new List<String>() { "Alex", "Nico", "Johann" };
                //test1.hiddenNames = new List<String>() { "name1", "name2", "name3", "name4", "name5" };
                testNode2.values = new int[3] { 0, 1, 2 };
                testNode2.Left = testNode1;
                testNode2.Right = testNode2;

                //DataStructureVisualizer.Visualize(testNode1);
                //List<String> testList2 = new List<String>() { };
                //DataStructureVisualizer.Visualize(testNode1, testList2);
            }

            

            //double linked list for testing
            {
                DoubleLinkedList testDoubleList = new DoubleLinkedList();
                testDoubleList.Insert("1");
                testDoubleList.Insert("2");
                testDoubleList.Insert("3");
                testDoubleList.Insert("4");
                testDoubleList.Insert("5");

                //DataStructureVisualizer.Visualize(testDoubleList);
                List<String> testList3 = new List<String>() { "Title" };
                DataStructureVisualizer.Visualize(testDoubleList, testList3);
                //DataStructureVisualizer.Visualize(testDoubleList);

            }
            
            
            //tests with list of nodes as member
            {
                Node testNode5 = new Node(0);
                Node testNode6 = new Node(1);
                Node testNode7 = new Node(2);
                Node testNode8 = new Node(3);

                testNode5.nodeList = new List<Node>();
                testNode5.nodeList.Add(testNode6);
                testNode5.nodeList.Add(testNode7);
                testNode5.nodeList.Add(testNode8);

                DataStructureVisualizer.Visualize(testNode5);

            }


            //tests with queue from collections
            {
                Queue<int> testQueue = new Queue<int>();
                testQueue.Enqueue(3);
                testQueue.Enqueue(2);
                testQueue.Enqueue(1);
                testQueue.Enqueue(4);

                DataStructureVisualizer.Visualize(testQueue, new List<String>() { "_array", "_item" });
            }

            //tests with skip list implementation 
            {


                CSKicksCollection.SkipList<int> testSkipList = new CSKicksCollection.SkipList<int>();
                Random rnd2 = new Random();
                for (int i = 0; i < 2; i++) testSkipList.Add(rnd2.Next(1, 10000));

                List<String> testList4 = new List<String>() {"Levels"};

                //TODO: fix so that blacklist gets checked at every point in the program
                List<String> testList41 = new List<string>() { "Value", "value", "Next", "next" };

                //DataStructureVisualizer.Visualize(testSkipList);
                //DataStructureVisualizer.Visualize(testSkipList, testList4);
                DataStructureVisualizer.Visualize(testSkipList, testList4, testList41);
            }
            
            //test with Student object
            {
                Student testStudent = new Student();
                StringWrapper[] temp = new StringWrapper[2];
                temp[0] = new StringWrapper("WS1");
                temp[1] = new StringWrapper("WS2");
                testStudent.wrappedStrings = temp;
                testStudent.wrappedString = new StringWrapper("WS§");
                testStudent.brother = new Student[]{new Student()};

                List<String> testList5 = new List<String>() {"brother", "content", "kidsList", "kids", "wrappedStrings", "wrappedString", "Info" };

                DataStructureVisualizer.Visualize(testStudent, testList5);
            }
            */

            //test with Person object
            {
                Person herbert = new Person("Herbert", 56);
                herbert.Spouse = new Person("Karin", 56);

                Person david = new Person("David", 22);
                Person fabian = new Person("Fabian", 24);

                herbert.kids.Add(david);
                herbert.kids.Add(fabian);

                Person alex = new Person("Alex", 24);
                Person nico = new Person("Nico", 20);

                david.friends.Add(alex);
                david.friends.Add(nico);

                herbert.friends.Add(alex);

                List<string> WL = new List<string>() { "kids", "Age", "Name", "friends"};

                DataStructureVisualizer.Visualize(herbert, WL);

            }

                
            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}
