using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataStructureVisualization
{
    /// <summary>
    /// DataStructureVisualizer implements a Method 'Visualize' which recurses the 
    /// data structure of an object and creates a DOT file containing graph description language
    /// which can be visualized with graphviz (graphviz.org). Visualization can be influenced by 
    /// passing names of members which will be displayed with value or be ignored while recursing;
    /// ----------------------------
    /// ###Created by David Kastner
    /// </summary>
    static class DataStructureVisualizer
    {
        //contains all objects that have already been processed
        private static Dictionary<Object, string> ProcessedNodes;

        //contains the names of members which should be displayed with values
        private static List<string> Whitelist;

        //contains the names of memebers which should be ignored while iterating data structure
        private static List<string> Blacklist;

        //strinbuilder to create nodes in the DOT file
        private static StringBuilder NodeBuilder;

        //stringbuilder to create edges at the end of the DOT file
        private static StringBuilder EdgeBuilder;

        //labeling internal nodes with id
        private static int internalNodeId = 0;
        private static int GetId() => internalNodeId++;

        /// <summary>
        /// Returns true if an object overrides the ToString() method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if overridden</returns>
        private static bool OverridesToString(dynamic obj)
        {
            //Console.WriteLine(obj.GetType().FullName);
            if (obj.ToString().Contains("`"))
            {
                return !obj.ToString().Remove(obj.ToString().IndexOf("`"))
                    .Equals(obj.GetType().FullName.Remove(obj.GetType().FullName.IndexOf("`")));
            }

            return !(obj.ToString().Equals(obj.GetType().FullName));
        }

        /// <summary>
        /// Visualize data structure of 'input'. Include all members recursively except
        /// for certain properties and backing fields obscuring the relevant information.
        /// Does not show values of members in the data structure except for those
        /// specifically named (case sensitive) in 'whitelistedMembers. Ignores members
        /// named in 'blacklistedMembers' and does not include members of those.
        /// </summary>
        /// <param name="input">object to be visualized</param>
        /// <param name="whitelistedMembers">members to display values</param>
        /// <param name="blacklistedMembers">members to be ignored</param>
        public static void Visualize(
            dynamic input,
            IEnumerable<string> whitelistedMembers = null,
            IEnumerable<string> blacklistedMembers = null)
        {
            //Initialize static members
            ProcessedNodes = new Dictionary<object, string>();
            Whitelist = new List<string>();
            Blacklist = new List<string>();

            NodeBuilder = new StringBuilder();
            EdgeBuilder = new StringBuilder();

            if (whitelistedMembers != null)
                foreach (var x in whitelistedMembers) Whitelist.Add(x);

            if (blacklistedMembers != null)
                foreach (var x in blacklistedMembers) Blacklist.Add(x);

            Blacklist.Add("k__BackingField");
            Blacklist.Add("m_value");
            Blacklist.Add("_firstChar");
            foreach (var x in typeof(String).GetProperties()) Blacklist.Add(x.Name);
            foreach (var x in typeof(String).GetFields()) Blacklist.Add(x.Name);

            StreamWriter SW = new StreamWriter("vis_" + input.GetType().Name + ".dot");

            SW.WriteLine("//created " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " by DataStructureVisualizer (K.D.)");
            SW.WriteLine("digraph " + Regex.Replace(input.GetType().Name.ToString(), "`", "") + " {\n rankdir=TB;");

            //destId for struct id, innerDestId for element in struct
            int destId = GetId();
            int innerId = 0;

            NodeBuilder.Append("struct"
                               + destId
                               + " [shape=record, style=filled fillcolor=\"0.6 0.3 1.000\", label=\""
                               + "{ <"
                               + innerId
                               + "> "
                               + input.GetType().Name);


            //save all recursive function calls to prevent writing in the wrong line
            var recursiveCalls = new List<Action>();

            //add object to prevent cycles
            ProcessedNodes.Add(input, new string(destId + ":" + innerId));

            //get all properties and call recursive function
            foreach (var property in input.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (property == null) continue;

                //needed because of nested loop
                bool skipMember = false;
                foreach (var entry in Blacklist)
                {
                    if (!Whitelist.Contains(property.Name) && property.Name.Contains(entry))
                    {
                        skipMember = true;
                        break;
                    }
                }
                if (skipMember) continue;

                innerId++;
                //to draw the edge from later on
                string source = new string(destId + ":" + innerId);

                //object behind member is null, create entry but don't follow it up
                if (property.GetValue(input) == null)
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + property.Name
                                       + " ");

                }
                else if (property.GetValue(input).GetType().IsValueType)
                {
                    if (Whitelist.Contains(property.Name))
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + property.Name
                                           + ": "
                                           + property.GetValue(input)
                                           + " ");
                    }
                    else
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + property.Name
                                           + " ");
                    }

                }
                //reference type
                else
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + property.Name
                                       + " ");

                    recursiveCalls.Add(new Action(() => VisualizeRecursively(input, property, source, destId)));
                }
            }

            //get all fields and call recursive function
            foreach (var field in input.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field == null) continue;

                bool skipMember = false;
                foreach (var entry in Blacklist)
                {
                    if (!Whitelist.Contains(field.Name) && field.Name.Contains(entry))
                    {
                        skipMember = true;
                        break;
                    }
                }

                if (skipMember) continue;

                innerId++;
                //to draw the edge from later on
                string source = new string(destId + ":" + innerId);

                //object behind member is null, create entry but don't follow it up
                if (field.GetValue(input) == null)
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + field.Name
                                       + " ");

                }
                else if (field.GetValue(input).GetType().IsValueType)
                {
                    if (Whitelist.Contains(field.Name))
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + field.Name
                                           + ": "
                                           + field.GetValue(input)
                                           + " ");
                    }
                    else
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + field.Name
                                           + " ");
                    }

                }
                //reference type
                else
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + field.Name
                                       + " ");

                    recursiveCalls.Add(new Action(() => VisualizeRecursively(input, field, source, destId)));
                }
            }

            foreach (var call in recursiveCalls)
            {
                call.Invoke();
            }

            NodeBuilder.AppendLine();
            NodeBuilder.Replace(System.Environment.NewLine, " } \" ]" + System.Environment.NewLine);
            SW.WriteLine(NodeBuilder);
            SW.Write(EdgeBuilder);
            SW.WriteLine("}");
            SW.Flush();
            SW.Close();

        }

        /// <summary>
        /// Inspect all properties and fields in object 'input' and calls this method again for every
        /// member. Creates nodes and edges in the DOT file for visualization. Whitelist, Blacklist and 
        /// ToString() implementation of members determines process by ignoring values except for those 
        /// named in Whitelist while skipping members named in Blacklist.
        /// </summary>
        /// <param name="input">object to which the member belongs</param>
        /// <param name="member">member to be handled for this method call</param>
        /// <param name="source">contains the source node and specific struct item (123:3)</param>"
        /// <param name="destId">current id to identify nodes for visualization</param>
        private static void VisualizeRecursively(dynamic input, dynamic member, string source, int destId)
        {

            //obj behind the reference
            var memberObj = member.GetValue(input);

            if (ProcessedNodes.TryGetValue(memberObj, out string processedNode))
            {
                EdgeBuilder.AppendLine("struct"
                                        + source
                                        + " -> "
                                        + "struct"
                                        + processedNode);
                return;
            }

            //save all recursive function calls to prevent writing in the wrong line
            var recursiveCalls = new List<Action>();

            destId = GetId();
            //create a node for the object behind the member
            NodeBuilder.AppendLine();
            NodeBuilder.Append("struct"
                               + destId
                               + " [shape=record label=\" { "
                               + memberObj.GetType().Name
                               + " ");

            EdgeBuilder.AppendLine("struct"
                                   + source
                                   + " -> "
                                   + "struct"
                                   + destId);

            int innerId = 0;

            //get all properties and call recursive function
            foreach (var property in memberObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (property == null) continue;

                //needed because of nested loop
                bool skipMember = false;
                foreach (var entry in Blacklist)
                {
                    if (!Whitelist.Contains(property.Name) && property.Name.Contains(entry))
                    {
                        skipMember = true;
                        break;
                    }
                }
                if (skipMember) continue;

                innerId++;
                //to draw the edge from later on
                string destination = new string(destId + ":" + innerId);

                //object behind member is null, create entry but don't follow it up
                if (property.GetValue(memberObj) == null)
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + property.Name
                                       + " ");

                }
                else if (property.GetValue(memberObj).GetType().IsValueType)
                {

                    if (Whitelist.Contains(property.Name))
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + property.Name
                                           + ": "
                                           + property.GetValue(memberObj)
                                           + " ");
                    }
                    else
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + property.Name
                                           + " ");
                    }

                }
                //reference type
                else
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + property.Name
                                       + " ");

                    recursiveCalls.Add(new Action(() => VisualizeRecursively(memberObj, property, destination, destId)));
                }
            }

            //get all fields and call recursive function
            foreach (var field in memberObj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field == null) continue;

                //needed because of nested loop
                bool skipMember = false;
                foreach (var entry in Blacklist)
                {
                    if (!Whitelist.Contains(field.Name) && field.Name.Contains(entry))
                    {
                        skipMember = true;
                        break;
                    }
                }
                if (skipMember) continue;

                innerId++;
                //to draw the edge from later on
                string destination = new string(destId + ":" + innerId);

                //object behind member is null, create entry but don't follow it up
                if (field.GetValue(memberObj) == null)
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + field.Name
                                       + " ");

                }
                else if (field.GetValue(memberObj).GetType().IsValueType)
                {

                    if (Whitelist.Contains(field.Name))
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + field.Name
                                           + ": "
                                           + field.GetValue(memberObj)
                                           + " ");
                    }
                    else
                    {
                        NodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + field.Name
                                           + " ");
                    }

                }
                //reference type
                else
                {
                    NodeBuilder.Append("| <"
                                       + innerId
                                       + "> "
                                       + field.Name
                                       + " ");

                    recursiveCalls.Add(new Action(() => VisualizeRecursively(memberObj, field, destination, destId)));
                }
            }

            foreach (var call in recursiveCalls)
            {
                call.Invoke();
            }

        }

    }
}
