using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataStructureVisualization
{
    static class DataStructureVisualizer
    {

        //contains all objects that have already been processed
        private static Dictionary<Object, int> ProcessedNodes;

        //contains the names of members which should be displayed with values
        private static List<string> Whitelist;

        //contains the names of memebers which should be ignored while iterating data structure
        private static List<string> Blacklist;

        //streamwriter to insert nodes in the DOT file
        private static StreamWriter SW;

        //stringbuilder to insert edges at the end of the DOT file
        private static StringBuilder SB;

        /// <summary>
        /// TODO: not finalized
        /// Check if an object has an overridden ToString method
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static bool IsOverridden(dynamic obj, string methodName)
        {
            /*
            Console.WriteLine("-----");
            Console.WriteLine(obj.ToString());
            Console.WriteLine(obj.GetType().FullName);
            */

            if (obj.ToString().Contains("`"))
            {
                //Console.WriteLine(obj.ToString().Remove(obj.ToString().IndexOf("`")));

                return !obj.ToString().Remove(obj.ToString().IndexOf("`"))
                    .Equals(obj.GetType().FullName.Remove(obj.GetType().FullName.IndexOf("`")));
            }

            return !(obj.ToString().Equals(obj.GetType().FullName));
        }

        private static void InitializeMembers()
        {
            ProcessedNodes = new Dictionary<object, int>();
            Whitelist = new List<string>();
            Blacklist = new List<string>();
            SB = new StringBuilder();
        }

        /// <summary>
        /// Visualize data structure of parameter. Include all members recursively except
        /// for certain properties and backing fields obscuring the relevant information
        /// Does not show values of members in the data structure
        /// </summary>
        /// <param name="input">object to be visualized</param>
        public static void Visualize(dynamic input)
        {
            List<string> emptyWhitelist = new List<string>();
            List<string> emptyBlacklist = new List<string>();
            Visualize(input, emptyWhitelist, emptyBlacklist);
        }

        /// <summary>
        /// Visualize data structure of parameter. Include all members recursively except
        /// for certain properties and backing fields obscuring the relevant information
        /// Does not show values of members in the data structure except for properties
        /// specifically named in the second parameter
        /// </summary>
        /// <param name="input">object to be visualized</param>
        /// <param name="whitelistedMembers">named members with displayed dat</param>
        public static void Visualize(dynamic input, IEnumerable<string> whitelistedMembers)
        {
            List<string> emptyBlacklist = new List<string>();
            Visualize(input, whitelistedMembers, emptyBlacklist);
        }



        /// <summary>
        /// Visualize data structure of parameter. Include all members recursively except
        /// for certain properties and backing fields obscuring the relevant information
        /// Does not show values of members in the data structure except for properties
        /// specifically named in the second parameter. Blocks and does not include members
        /// with a name included in the second parameter
        /// </summary>
        /// <param name="input">object to be visualized</param>
        /// <param name="whitelistedMembers">named members with displayed dat</param>
        public static void Visualize(dynamic input, IEnumerable<string> whitelistedMembers, IEnumerable<string> blacklistedMembers)
        {
            InitializeMembers();

            //get dynamic type of the input object
            Type inputType = input.GetType();

            SW = new StreamWriter("vis_" + inputType.Name + ".dot");

            foreach (var x in whitelistedMembers) Whitelist.Add(x);
            foreach (var x in blacklistedMembers) Blacklist.Add(x);

            Blacklist.Add("k__BackingField");
            Blacklist.Add("m_value");
            Blacklist.Add("_firstChar");
            foreach (var x in typeof(String).GetProperties()) Blacklist.Add(x.Name);
            foreach (var x in typeof(String).GetFields()) Blacklist.Add(x.Name);



            SW.WriteLine("//created " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " by DataStructureVisualizer (K.D.)\n");
            SW.WriteLine("digraph " + Regex.Replace(inputType.Name.ToString(), "`", "") + " {\n rankdir=TB;\n");

            //sourceId and destId to distinguish nodes and draw edges between nodes
            int sourceId = 0;
            int destId = 0;

            SW.WriteLine("struct" + destId + " [shape=box3d, style=filled fillcolor=\"0.6 0.3 1.000\", label=\"" + inputType.Name + "\"];");
            ProcessedNodes.Add(input, destId);

            try
            {
                //get all properties and call recursive function
                foreach (var property in inputType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (property != null) VisualizeRecursively(input, property, sourceId, ref destId);

                //get all fields and call recursive function
                foreach (var field in inputType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (field != null) VisualizeRecursively(input, field, sourceId, ref destId);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                SW.WriteLine(SB + "}");
                SW.Flush();
                SW.Close();
            }

        }

        /// <summary>
        /// recursive function handling a member of the 'input' param
        /// creates nodes and edges in DOT notation for graphViz
        /// </summary>
        /// <param name="input">object to which the member belongs</param>
        /// <param name="member">member to be handled for this method call</param>
        /// <param name="sourceId">source node to draw the edge from</param>
        /// <param name="destId">current id to identify nodes for visualization</param>
        private static void VisualizeRecursively(dynamic input, dynamic member, int sourceId, ref int destId)
        {
            //exclude duplicate information stored in backing fields except for whitelisted member names
            if (Blacklist.Any(entry => member.Name.Contains(entry)) && !Whitelist.Contains(member.Name))
            {
                Console.WriteLine("excluded member " + member.Name);
                return;
            }


            try
            {
                //member is null, exclude these eventually
                if (member.GetValue(input) == null)
                {
                    /*
                    destId++;
                    //creating node
                    SW.WriteLine("struct"
                                 + destId
                                 + " [shape=record style=filled fillcolor=\"0.8 0.1 1.000\", label=\""
                                 + member.Name + " (null) "
                                 + "\"];");

                    //creating edge from the source node
                    SB.AppendLine("struct"
                                  + sourceId
                                  + " -> "
                                  + "struct"
                                  + destId);

                    */
                    return;
                }

                //obj has already been processed, create edge backwards
                if (ProcessedNodes.TryGetValue(member.GetValue(input), out int tempDestination))
                {
                    destId++;
                    //creating node
                    SW.WriteLine("struct"
                                 + destId
                                 + " [shape=record label=\""
                                 + member.Name
                                 + "\"];");

                    //creating edge from the source node
                    SB.AppendLine("struct"
                                  + sourceId
                                  + " -> "
                                  + "struct"
                                  + destId);

                    //creating edge back to the existing node
                    SB.AppendLine("struct"
                                  + destId
                                  + " -> "
                                  + "struct"
                                  + tempDestination);
                    return;
                }

                //add to processedNodes
                ProcessedNodes.Add(member.GetValue(input), destId + 1);

                //handle Enumerables and avoid splitting up String into chars etc
                if (member.GetValue(input) is IEnumerable && !IsOverridden(member.GetValue(input), "ToString"))
                {

                    if (!Whitelist.Contains(member.Name))
                    {
                        destId++;
                        //creating node
                        SW.WriteLine("struct"
                                     + destId
                                     + "[shape = folder, style = filled fillcolor =\"0.0 0.0 1.000\", label=\""
                                     + member.Name
                                     + "\"];");

                        //creating edge from the source node
                        SB.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);
                    }
                    else
                    {
                        destId++;
                        //creating node
                        SW.WriteLine("struct"
                                     + destId
                                     + "[shape = folder, style = filled fillcolor =\"0.2 0.2 1.000\", label=\""
                                     + member.Name
                                     + "\"];");

                        //creating edge from the source node
                        SB.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);

                        sourceId = destId;

                        //iterate the IEnumerable and process each entry
                        foreach (var entry in member.GetValue(input))
                        {
                            //ToString() is overridden, display values
                            if (IsOverridden(entry, "ToString"))
                            {
                                destId++;
                                //creating node
                                SW.WriteLine("struct"
                                             + destId
                                             + "[shape = record, style = filled fillcolor =\"0.2 0.2 1.000\", label=\""
                                             + entry.ToString()
                                             + "\"];");

                                //creating edge from the source node
                                SB.AppendLine("struct"
                                              + sourceId
                                              + " -> "
                                              + "struct"
                                              + destId);

                            }
                            //ToString() is not overriden, iterate properties/fields of entry recursively
                            else
                            {

                                //get all properties and call recursive function
                                foreach (var property in entry.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                    if (property != null) VisualizeRecursively(entry, property, sourceId, ref destId);

                                //get all fields and call recursive function
                                foreach (var field in entry.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                    if (field != null) VisualizeRecursively(entry, field, sourceId, ref destId);
                            }

                        }
                    }

                    return;
                }

                //display and iterate whitelisted members
                if (Whitelist.Contains(member.Name))
                {
                    if (IsOverridden(member.GetValue(input), "ToString"))
                    {
                        destId++;
                        //creating node
                        SW.WriteLine("struct"
                                     + destId
                                     + " [shape=record style=filled fillcolor=\"0.2 0.2 1.000\", label=\""
                                     + member.Name
                                     + " | "
                                     + member.GetValue(input)
                                     + "\"];");

                        //creating edge
                        SB.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);
                    }
                    else
                    {
                        destId++;
                        //creating node
                        SW.WriteLine("struct"
                                     + destId
                                     + " [shape=record style=filled fillcolor=\"0.2 0.2 1.000\", label=\""
                                     + member.Name
                                     + "\"];");

                        //creating edge
                        SB.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);
                    }

                    //get the actual object behind the member
                    var memberObject = member.GetValue(input);

                    if (memberObject != null)
                    {
                        sourceId = destId;

                        //get all properties and call recursive function
                        foreach (var property in memberObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (property != null) VisualizeRecursively(memberObject, property, sourceId, ref destId);

                        //get all fields and call recursive function
                        foreach (var field in memberObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (field != null) VisualizeRecursively(memberObject, field, sourceId, ref destId);

                    }
                }

                //not whitelisted members
                else
                {
                    destId++;
                    //creating node
                    SW.WriteLine("struct"
                                 + destId
                                 + " [shape=record, label=\""
                                 + member.Name
                                 + "\"];");

                    //creating edge from the source node
                    SB.AppendLine("struct"
                                  + sourceId
                                  + " -> "
                                  + "struct"
                                  + destId);


                    sourceId = destId;

                    //get the actual object behind the member
                    var memberObject = member.GetValue(input);

                    //TODO: figure out which data types not to inspect further (String etc)
                    //&& !(IsOverridden(memberObject, "ToString"))

                    //only go until the object can be displayed
                    if (memberObject != null )
                    {
                        //get all properties and call recursive function
                        foreach (var property in memberObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (property != null) VisualizeRecursively(memberObject, property, sourceId, ref destId);

                        //get all fields and call recursive function
                        foreach (var field in memberObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (field != null) VisualizeRecursively(memberObject, field, sourceId, ref destId);
                    }

                }

            }
            //indexed member
            catch (TargetParameterCountException exc)
            {
                Console.WriteLine(exc.Message);
            }
        }

    }
}
