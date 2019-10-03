using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataStructureVisualization
{
    static class DataStructureVisualizer
    {

        //contains all objects that have already been processed
        private static Dictionary<Object, int> ProcessedNodes = new Dictionary<Object, int>();

        //contains the names of members which should be displayed with values
        private static List<String> Whitelist = new List<string>();

        //contains the names of memebers which should be ignored while iterating data structure
        private static List<String> Blacklist = new List<string>();

        //streamwriter to insert nodes in the DOT file
        private static StreamWriter SW;

        //stringbuilder to insert edges at the end of the DOT file
        private static StringBuilder SB = new StringBuilder();


        /// <summary>
        /// overload to display the values of members named in the IEnumerable
        /// </summary>
        /// <param name="input">object whose data structure is being visualized</param>
        /// <param name="whitelistedMembers">names of members whose values will be visualized</param>
        public static void Visualize(dynamic input, IEnumerable<String> whitelistedMembers)
        {
            foreach (var name in whitelistedMembers) Whitelist.Add(name);
            Visualize(input);
        }

        /// <summary>
        /// generates a DOT file to display the data structure and values of members of the passed object 'input'
        /// </summary>
        /// <param name="input">object whose data structure is being visualized</param>
        public static void Visualize(dynamic input)
        {

            //get dynamic type of the input object
            Type inputType = input.GetType();

            Blacklist.Add("k__BackingField");
            Blacklist.Add("Item");
            Blacklist.Add("_value");

            SW = new StreamWriter("vis_" + inputType.Name + ".dot");

            SW.WriteLine("//created " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " by DataStructureVisualizer (K.D.)\n");
            SW.WriteLine("digraph " + Regex.Replace(inputType.Name.ToString(), "`", "") + " {\n rankdir=TB;\n");

            //sourceId and destId to distinguish nodes and draw edges between nodes
            int sourceId = 0;
            int destId = 0;

            SW.WriteLine("struct" + destId + " [shape=box3d, style=filled fillcolor=\"0.6 0.3 1.000\", label=\"" + inputType.Name + "\"];");
            ProcessedNodes.Add(input, destId);

            //TODO: exception handling to make sure the DOT file gets closed appropriately

            //try
            {
                //get all properties and call recursive function
                foreach (var property in inputType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (property != null) VisualizeRecursively(input, property, sourceId, ref destId);

                //get all fields and call recursive function
                foreach (var field in inputType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (field != null) VisualizeRecursively(input, field, sourceId, ref destId);

            }
            //catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                //throw e;
            }
            //finally
            {
                SW.WriteLine(SB.ToString() + "}");
                SW.Flush();
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
            //Get information specific to member type and determine type for later
            String memberType;
            String memberTypeName;

            //member param is a Property
            if (Equals("RuntimePropertyInfo", member.GetType().Name))
            {
                memberType = "Property";
                memberTypeName = member.PropertyType.Name;
            }

            //member param is a Field
            else if (Equals("RtFieldInfo", member.GetType().Name))
            {
                memberType = "Field";
                memberTypeName = member.FieldType.Name;
            }
            //i.e. Methods are not supported
            else return;

            //exclude duplicate information stored in backing fields except for whitelisted member names
            //need to find a stable workaround eventually with Blacklist
            //TODO: include whitelist in this
            foreach (var entry in Blacklist) if (member.Name.Contains(entry)) return;

            destId++;

            //indexed members have to be treated differently
            if (member.GetValue(input) is IList)
            {
                VisualizeIList(input, member, memberType, sourceId, ref destId);
            }
            //is not an IList, member.GetValue(input) can be called
            else
            {
                //if a node has already been processed, only draw the edge
                int tempDestination = 0;
                if (member.GetValue(input) != null && ProcessedNodes.TryGetValue(member.GetValue(input), out tempDestination))
                {
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


                if (member.GetValue(input) == null)
                {
                    //not sure if this should be shown at all

                    //creating node
                    SW.WriteLine("struct"
                                 + destId
                                 + " [shape=record style=filled fillcolor=\"0.8 0.1 1.000\", label=\""
                                 + member.Name
                                 + " (null)"
                                 + "\"];");

                    //creating edge from the source node
                    SB.AppendLine("struct"
                                  + sourceId
                                  + " -> "
                                  + "struct"
                                  + destId);

                    return;
                }


                //add to processedNodes
                ProcessedNodes.Add(member.GetValue(input), destId);

                //visualizes this member's value if "ShowData" is set (non-indexed members)
                //currently not supporting the Attribute ShowData version
                //if (Attribute.IsDefined(member, typeof(ShowData)) || Whitelist.Contains(memberName))

                if (Whitelist.Contains(member.Name))
                {

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

                    //when ToString() is not overriden it returns the .GetType().FullName and the name if possible
                    //cut away the name and check if ToString() has been overriden
                    if (member.ToString().Remove(member.ToString().IndexOf(" ")).Equals(member.GetValue(input).GetType().FullName))
                    {
                        //get the actual object behind the member
                        var memberObject = member.GetValue(input);

                        if (memberObject != null && !(memberObject is String))
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

                }
                //ShowData attribute not set and no indexed member
                else
                {
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

                    //if ToString() is not overriden, go deeper
                    //can be checked by checking what gets returned by ToString()
                    if (member.ToString().Remove(member.ToString().IndexOf(" ")).Equals(member.GetValue(input).GetType().FullName))
                    {
                        sourceId = destId;

                        //get the actual object behind the member
                        var memberObject = member.GetValue(input);

                        //TODO: strings would get split into characters, need to find solution for such cases
                        if (memberObject != null && !(memberObject is String))
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

            }
        }


        /// <summary>
        /// visualizes indexed members and calls VisualizeRecursively for each handled member
        /// i.e. Array or List<>
        /// </summary>
        /// <param name="input">object to which the member belongs</param>
        /// <param name="member">member to be handled for this method call</param>
        /// <param name="memberType">type of member easier to determine beforehand</param>
        /// <param name="sourceId">source node to draw the edge from</param>
        /// <param name="destId">current id to identify nodes for visualization</param>
        private static void VisualizeIList(dynamic input, dynamic member, String memberType, int sourceId, ref int destId)
        {
            //exclude duplicate information stored in backing fields except for whitelisted member names
            //need to find a stable workaround eventually with Blacklist
            //TODO: include whitelist in this
            foreach (var entry in Blacklist) if (member.Name.Contains(entry)) return;

            Type objType;
            Type itemType = typeof(String);
            if (memberType.Equals("Field")) objType = member.FieldType;
            else if (memberType.Equals("Property")) objType = member.PropertyType;
            else return;

            int containerLength = 0;
            if (objType.IsArray)
                containerLength = member.GetValue(input).Length;
            else
                containerLength = member.GetValue(input).Count;

            //itemType not important as of this version
            //if (member.GetValue(input)[containerLength] != null) itemType = member.GetValue(input)[containerLength - 1].GetType();


            //if attribute is not set, only display length/count of elements and name
            //not currently supporting the ShowData attribute, only named in Whitelist
            //if (!Attribute.IsDefined(member, typeof(ShowData)) && !Whitelist.Contains(member.Name))

            //member not named in Whitelist, display only length of IList and do not go deeper
            if (!Whitelist.Contains(member.Name))
            {
                //creating node
                SW.Write("struct" + destId + " [shape=record, label=\"");
                SW.Write(""
                         + member.Name
                         + " | Length: "
                         + containerLength);
                SW.Write("\"];\n");

                //creating edge
                SB.AppendLine("struct"
                              + sourceId
                              + " -> "
                              + "struct"
                              + destId);
            }

            //member named in Whitelist, create node for IList and call VisualizeRecursively for each item in IList
            else
            {
                //creating node
                SW.Write("struct"
                         + destId
                         + " [shape=record style=filled fillcolor=\"0.2 0.2 1.000\", label=\""
                         + member.Name
                         + "\"];\n");

                //creating edge
                SB.AppendLine("struct"
                              + sourceId
                              + " -> "
                              + "struct"
                              + destId);

                //for each item the source node 'IList' needs to be used to draw edge
                int enumId = destId;

                foreach (var entry in member.GetValue(input))
                {
                    sourceId = destId;
                    //if ToString() is overridden create a node displaying the value with ToString(), do not go deeper
                    //can be checked by checking what gets returned by ToString()
                    if (!entry.ToString().Remove(entry.ToString().IndexOf(" ")).Equals(entry.GetValue(input).GetType().FullName))
                    {
                        destId++;
                        //create node
                        SW.Write("struct"
                                 + destId
                                 + " [shape=record, label=\""
                                 + entry
                                 + "\"];\n");

                        //creating edge
                        SB.AppendLine("struct"
                                      + enumId
                                      + " -> "
                                      + "struct"
                                      + sourceId);

                    }
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
        }

    }
}
