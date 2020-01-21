using System;
using System.Collections;
using System.Collections.Generic;

namespace DataStructureVisualization
{
    class Program
    {
        static void Main(string[] args)
        {
            Random rnd = new Random();
            AVL avlTree = new AVL();

            for (int i = 0; i < 24; i++)
            {
                avlTree.Add(rnd.Next(0, 10000));
            }

            List<String> testWhiteList1 = new List<String>() { "data" };
            DataStructureVisualizer.Visualize(avlTree, testWhiteList1);

            //binary tree
            /*
            {
                BinaryTree testTree = new BinaryTree();
                Random rnd = new Random();
                int initVal = 30;
                int count = 0;
                testTree.Insert(30);
                for (int i = 0; i < 10; i++)
                {
                    testTree.Insert(initVal + i);
                    testTree.Insert(initVal - i);
                }
                //testTree.DisplayTree();

                List<String> testWhiteList1 = new List<String>() {"Data"};
                List<String> testBlackList1 = new List<string>() { "duplicateData" };
                DataStructureVisualizer.Visualize(testTree, testWhiteList1, testBlackList1);
            }
            */

            //single linked list (not done)
            {
                SingleLinkedList singleLinkedList = new SingleLinkedList();
                singleLinkedList.Insert(100);
                singleLinkedList.Insert(200);
                singleLinkedList.Insert(300);

                List<String> testWhiteList12 = new List<String>() { "data" };
                DataStructureVisualizer.Visualize(singleLinkedList, testWhiteList12);
            }

            //double linked list for testing
            {
                DoubleLinkedList testDoubleList = new DoubleLinkedList();
                testDoubleList.Insert(3);
                testDoubleList.Insert(2);
                testDoubleList.Insert(1);

                DataStructureVisualizer.Visualize(testDoubleList, testWhiteList1);
            }


            //tests with skip list implementation 
            {

                CSKicksCollection.SkipList<int> testSkipList = new CSKicksCollection.SkipList<int>();
                Random rnd2 = new Random();
                for (int i = 0; i < 5; i++) testSkipList.Add(rnd2.Next(1, 10000));


                List<String> testWL1 = new List<String>() { "Levels", "MaxLevels" };
                List<String> testBL1 = new List<string>() { "random", "size" };

                DataStructureVisualizer.Visualize(testSkipList, testWL1, testBL1);
            }



            //test with Person object
            {
                Person herbert = new Person("Herbert", 56);
                Person karin = new Person("Karin", 56);
                herbert.Spouse = karin;

                Person david = new Person("David", 22);
                Person fabian = new Person("Fabian", 24);

                Person alex = new Person("Alex", 24);
                Person nico = new Person("Nico", 20);

                herbert.kids.Add(david);
                herbert.kids.Add(fabian);
                karin.kids.Add(david);
                karin.kids.Add(fabian);
                karin.friends.Add(alex);

                david.friends.Add(alex);
                david.friends.Add(nico);

                herbert.friends.Add(alex);

                Person testPerson = new Person("David", 22);

                IEnumerable<string> whiteList = new List<string>() { "Age", "Name", "friends" };
                DataStructureVisualizer.Visualize(herbert, whiteList);


                PersonDB personDB = new PersonDB();
                personDB.Data = new Person[3];
                personDB.Data[0] = herbert;
                personDB.Data[1] = alex;
                personDB.Data[2] = nico;

                DataStructureVisualizer.Visualize(personDB);

            }

            //test with queue
            {
                Queue<Person> testQueue = new Queue<Person>();

                var hans = new Person("Hans", 22);
                var david = new Person("David", 22);
                var felix = new Person("Felix", 21);
                var susi = new Person("Susi", 20);
                hans.Spouse = susi;
                susi.friends.Add(david);
                susi.friends.Add(felix);
                david.friends.Add(felix);
                david.friends.Add(hans);
                david.friends.Add(susi);


                testQueue.Enqueue(david);
                testQueue.Enqueue(felix);
                testQueue.Enqueue(hans);


                List<string> queueWL = new List<string>() { "_array" };
                DataStructureVisualizer.Visualize(testQueue);

            }

            //test with Collections.List
            {
                List<int> testlist = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
                List<string> whitelist = new List<string>() { "_items" };
                DataStructureVisualizer.Visualize(testlist);
            }

            //test with array
            {
                int[] testArr = new int[10];
                for (int i = 0; i < 10; i++) testArr[i] = i;

                DataStructureVisualizer.Visualize(testArr);

            }




            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}
