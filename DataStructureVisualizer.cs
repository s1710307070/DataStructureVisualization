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
        private static Dictionary<Object, int> ProcessedNodes;

        //contains the names of members which should be displayed with values
        private static List<string> Whitelist;

        //contains the names of memebers which should be ignored while iterating data structure
        private static List<string> Blacklist;

        //strinbuilder to create nodes in the DOT file
        private static StringBuilder NodeBuilder;

        //stringbuilder to create edges at the end of the DOT file
        private static StringBuilder EdgeBuilder;

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
            ProcessedNodes = new Dictionary<object, int>();
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

            //get dynamic type of the input object
            Type inputType = input.GetType();

            StreamWriter SW = new StreamWriter("vis_" + inputType.Name + ".dot");

            SW.WriteLine("//created " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " by DataStructureVisualizer (K.D.)");
            SW.WriteLine("digraph " + Regex.Replace(inputType.Name.ToString(), "`", "") + " {\n rankdir=TB;");

            //sourceId and destId to distinguish nodes and draw edges between nodes
            int sourceId = 0;
            int destId = 0;
            int innerId = 0;

            NodeBuilder.Append("struct"
                               + destId
                               + " [shape=record, style=filled fillcolor=\"0.6 0.3 1.000\", label=\""
                               + "{ <"
                               + innerId
                               + "> "
                               + inputType.Name);

            ProcessedNodes.Add(input, destId);

            try
            {
                //get all properties and call recursive function
                foreach (var property in inputType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (property != null)
                    {
                        if (Blacklist.Contains(property.Name)) continue;
                        if (property.GetValue(input) == null) continue;
                        if (property.GetValue(input).GetType().IsValueType)
                        {
                            if (Whitelist.Contains(property.Name))
                            {
                                innerId++;
                                NodeBuilder.Append("| <"
                                                   + innerId
                                                   + "> "
                                                   + property.Name
                                                   + " | "
                                                   + property.GetValue(input)
                                                   + " } ");
                            }
                            else
                            {
                                innerId++;
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
                            innerId++;
                            NodeBuilder.Append("| <"
                                               + innerId
                                               + "> "
                                               + property.Name
                                               + " ");


                            sourceId = destId;
                            VisualizeRecursively(input, property, sourceId, innerId, ref destId);
                        }
                    }
                }


                //get all fields and call recursive function
                foreach (var field in inputType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field != null)
                    {
                        if (Blacklist.Contains(field.Name)) continue;
                        if (field.GetValue(input) == null) continue;
                        if (field.GetValue(input).GetType().IsValueType)
                        {
                            if (Whitelist.Contains(field.Name))
                            {
                                innerId++;
                                NodeBuilder.Append("| <"
                                                   + innerId
                                                   + "> "
                                                   + field.Name
                                                   + " | "
                                                   + field.GetValue(input)
                                                   + " } ");
                            }
                            else
                            {
                                innerId++;
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
                            innerId++;
                            NodeBuilder.Append("| <"
                                               + innerId
                                               + "> "
                                               + field.Name
                                               + " ");

                            sourceId = destId;
                            VisualizeRecursively(input, field, sourceId, innerId, ref destId);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                NodeBuilder.AppendLine();
                NodeBuilder.Replace(System.Environment.NewLine, "} \"]" + System.Environment.NewLine);
                SW.WriteLine(NodeBuilder);
                SW.Write(EdgeBuilder);
                SW.WriteLine("}");
                SW.Flush();
                SW.Close();
            }

        }

        /// <summary>
        /// Inspect all properties and fields in object 'input' and calls this method again for every
        /// member. Creates nodes and edges in the DOT file for visualization. Whitelist, Blacklist and 
        /// ToString() implementation of members determines process by ignoring values except for those 
        /// named in Whitelist while skipping members named in Blacklist.
        /// </summary>
        /// <param name="input">object to which the member belongs</param>
        /// <param name="member">member to be handled for this method call</param>
        /// <param name="sourceId">source node to draw the edge from</param>
        /// <param name="innerSourceId">inner id of source node to draw edge from</param>"
        /// <param name="destId">current id to identify nodes for visualization</param>
        private static void VisualizeRecursively(dynamic input, dynamic member, int sourceId, int innerSourceId, ref int destId)
        {

            try
            {
                //member is null, exclude these eventually
                if (member.GetValue(input) == null)
                {
                    destId++;
                    //creating node
                    NodeBuilder.AppendLine("struct"
                                 + destId
                                 + " [shape=point style=filled fillcolor=\"0.8 0.1 1.000\", label=\""
                                 + member.GetType()
                                 + "\"];");

                    //creating edge from the source node
                    EdgeBuilder.AppendLine("struct"
                                  + sourceId
                                  + ":" + innerSourceId
                                  + " -> "
                                  + "struct"
                                  + destId);

                    return;
                }

                //obj has already been processed, create edge backwards
                if (ProcessedNodes.TryGetValue(member.GetValue(input), out int tempDestination))
                {
                    destId++;
                    //creating node
                    NodeBuilder.AppendLine("struct"
                                 + destId
                                 + " [shape=record label=\""
                                 + member.GetType()
                                 + "\"];");

                    //creating edge from the source node
                    //todo: not sure if this is right
                    EdgeBuilder.AppendLine("struct"
                                  + sourceId
                                  + ":" + innerSourceId
                                  + " -> "
                                  + "struct"
                                  + destId);

                    //creating edge back to the existing node
                    EdgeBuilder.AppendLine("struct"
                                  + destId
                                  + " -> "
                                  + "struct"
                                  + tempDestination);
                    return;
                }

                //obj behind the reference
                var memberObj = member.GetValue(input);

                //add to processedNodes if it is not a simple type
                if (!memberObj.GetType().IsPrimitive) ProcessedNodes.Add(memberObj, destId + 1);

                int innerDestId = 0;
                destId++;

                NodeBuilder.AppendLine();
                NodeBuilder.Append("struct"
                                   + destId
                                   + " [shape=record label=\" {"
                                   + " <"
                                   + innerDestId
                                   + "> "
                                   + memberObj.GetType().Name
                                   + " ");

                EdgeBuilder.AppendLine("struct"
                                       + sourceId
                                       + ":"
                                       + innerSourceId
                                       + " -> "
                                       + "struct"
                                       + destId);

                //get all properties and call recursive function
                foreach (var property in memberObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (property != null)
                    {
                        if (Blacklist.Contains(property.Name)) continue;
                        if (property.GetValue(input) == null) continue;
                        if (property.GetValue(input).GetType().IsValueType)
                        {
                            if (Whitelist.Contains(property.Name))
                            {
                                innerDestId++;
                                NodeBuilder.Append("| <"
                                                   + innerDestId
                                                   + "> "
                                                   + property.Name
                                                   + " | "
                                                   + property.GetValue(memberObj)
                                                   + " } ");
                            }
                            else
                            {
                                innerDestId++;
                                NodeBuilder.Append("| <"
                                                   + innerDestId
                                                   + "> "
                                                   + property.Name
                                                   + " ");
                            }

                        }
                        //reference type
                        else
                        {
                            innerDestId++;
                            NodeBuilder.Append("| <"
                                               + innerDestId
                                               + "> "
                                               + property.Name
                                               + " ");

                            sourceId = destId;

                            VisualizeRecursively(memberObj, property, sourceId, innerDestId, ref destId);
                        }
                    }
                }

                //get all fields and call recursive function
                foreach (var field in memberObj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field != null)
                    {
                        if (Blacklist.Contains(field.Name)) continue;
                        if (field.GetValue(input) == null) continue;
                        if (field.GetValue(input).GetType().IsValueType)
                        {
                            if (Whitelist.Contains(field.Name))
                            {
                                innerDestId++;
                                NodeBuilder.Append("| <"
                                                   + innerDestId
                                                   + "> "
                                                   + field.Name
                                                   + " | "
                                                   + field.GetValue(memberObj)
                                                   + " } ");
                            }
                            else
                            {
                                innerDestId++;
                                NodeBuilder.Append("| <"
                                                   + innerDestId
                                                   + "> "
                                                   + field.Name
                                                   + " ");
                            }

                        }
                        //reference type
                        else
                        {
                            innerDestId++;
                            NodeBuilder.Append("| <"
                                               + innerDestId
                                               + "> "
                                               + field.Name
                                               + " ");

                            sourceId = destId;

                            VisualizeRecursively(memberObj, field, sourceId, innerDestId, ref destId);
                        }
                    }
                }





                /*
                //handle Enumerables and avoid splitting up String into chars etc
                if (member.GetValue(input) is IEnumerable && !OverridesToString(member.GetValue(input)))
                {
                    
                    if (!Whitelist.Contains(member.Name))
                    {
                        destId++;
                        //creating node
                        NodeBuilder.WriteLine("struct"
                                     + destId
                                     + "[shape = folder, style = filled fillcolor =\"0.0 0.0 2.000\", label=\""
                                     + member.Name
                                     + "\"];");

                        //creating edge from the source node
                        EdgeBuilder.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);
                    }
                    else
                    {
                        destId++;
                        //creating node
                        NodeBuilder.WriteLine("struct"
                                     + destId
                                     + "[shape = folder, style = filled fillcolor =\"0.6 0.3 1.000\", label=\""
                                     + member.Name
                                     + "\"];");

                        //creating edge from the source node
                        EdgeBuilder.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);


                        //iterate the IEnumerable and process each entry
                        int enumId = destId;
                        int entryCount = 0;

                        foreach (var entry in member.GetValue(input))
                        {
                            //ToString() is overridden, display values
                            if (OverridesToString(entry))
                            {
                                destId++;
                                //creating node
                                NodeBuilder.WriteLine("struct"
                                             + destId
                                             + "[shape = record, style = filled fillcolor =\"0.2 0.2 1.000\", label=\""
                                             + entry.ToString()
                                             + "\"];");

                                //creating edge from the source node
                                EdgeBuilder.AppendLine("struct"
                                              + enumId
                                              + " -> "
                                              + "struct"
                                              + destId);

                            }
                            //ToString() is not overriden, recurse properties/fields of entry
                            //create parent node for the entry object
                            else
                            {

                                destId++;
                                entryCount++;
                                //creating node
                                NodeBuilder.WriteLine("struct"
                                             + destId
                                             + "[shape = record, style = filled fillcolor =\"0.2 0.2 1.000\", label=\""
                                             + "entry_" + entryCount
                                             + "\"];");

                                //creating edge from the source node
                                EdgeBuilder.AppendLine("struct"
                                              + enumId
                                              + " -> "
                                              + "struct"
                                              + destId);


                                sourceId = destId;

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
                    if (OverridesToString(member.GetValue(input)))
                    {
                        destId++;
                        //creating node
                        NodeBuilder.WriteLine("struct"
                                     + destId
                                     + " [shape=record style=filled fillcolor=\"0.2 0.2 1.000\", label=\""
                                     + member.Name
                                     + " | "
                                     + member.GetValue(input)
                                     + "\"];");

                        //creating edge
                        EdgeBuilder.AppendLine("struct"
                                      + sourceId
                                      + " -> "
                                      + "struct"
                                      + destId);
                    }
                    else
                    {
                        destId++;
                        //creating node
                        NodeBuilder.WriteLine("struct"
                                     + destId
                                     + " [shape=record style=filled fillcolor=\"0.2 0.2 1.000\", label=\""
                                     + member.Name
                                     + "\"];");

                        //creating edge
                        EdgeBuilder.AppendLine("struct"
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
                    NodeBuilder.WriteLine("struct"
                                 + destId
                                 + " [shape=record, label=\""
                                 + member.Name
                                 + "\"];");

                    //creating edge from the source node
                    EdgeBuilder.AppendLine("struct"
                                  + sourceId
                                  + " -> "
                                  + "struct"
                                  + destId);


                    sourceId = destId;

                    //get the actual object behind the member
                    var memberObject = member.GetValue(input);

                    //only go until the object can be displayed
                    if (memberObject != null)
                    {
                        //get all properties and call recursive function
                        foreach (var property in memberObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (property != null) VisualizeRecursively(memberObject, property, sourceId, ref destId);

                        //get all fields and call recursive function
                        foreach (var field in memberObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            if (field != null) VisualizeRecursively(memberObject, field, sourceId, ref destId);
                    }

                }

                */
            }

            //indexed member
            catch (Exception exc)
            {
                Console.WriteLine("-----------");
                Console.WriteLine(member.Name);
                Console.WriteLine(exc.GetType() + exc.Message);
            }
        }

    }
}
