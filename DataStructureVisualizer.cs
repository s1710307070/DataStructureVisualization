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
        private static Dictionary<object, string> _processedNodes;

        //contains the names of members which should be displayed with values
        private static List<string> _whitelist;

        //contains the names of members which should be ignored while iterating data structure
        private static List<string> _blacklist;

        //stringbuilder to create nodes in the DOT file
        private static StringBuilder _nodeBuilder;

        //stringbuilder to create edges at the end of the DOT file
        private static StringBuilder _edgeBuilder;

        //labeling internal nodes with id
        private static uint _internalNodeId = 0;
        private static uint GetId() => _internalNodeId++;

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
        /// containing blacklisted substrings.
        /// </summary>
        /// <param name="input">object to be visualized</param>
        /// <param name="whitelistedMembers">members named in whitelist will display values</param>
        /// <param name="blacklistedMembers">members containing blacklisted substrings will be ignored</param>
        public static void Visualize(
            dynamic input,
            IEnumerable<string> whitelistedMembers = null,
            IEnumerable<string> blacklistedMembers = null)
        {
            //passed a null object to visualize
            if (input == null) throw new ArgumentNullException(nameof(input));

            //Initialize static members
            //static members documented at declaration
            _processedNodes = new Dictionary<object, string>();

            _whitelist = new List<string>();
            _blacklist = new List<string>();

            _nodeBuilder = new StringBuilder();
            _edgeBuilder = new StringBuilder();

            if (whitelistedMembers != null)
                foreach (var x in whitelistedMembers)
                    _whitelist.Add(x);

            if (blacklistedMembers != null)
                foreach (var x in blacklistedMembers)
                    _blacklist.Add(x);

            _blacklist.Add("k__BackingField");
            _blacklist.Add("m_value");
            _blacklist.Add("_firstChar");
            foreach (var x in typeof(string).GetProperties()) _blacklist.Add(x.Name);
            foreach (var x in typeof(string).GetFields()) _blacklist.Add(x.Name);

            var streamWriter = new StreamWriter("vis_" + input.GetType().Name + ".dot");

            streamWriter.WriteLine(
                "//created " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " by DataStructureVisualizer (Kastner David)");

            streamWriter.Write(
                "digraph " + Regex.Replace(input.GetType().Name.ToString(), "`", "") + " {\n rankdir=TB;");

            //begin visualization with input object
            //todo: add try catch here with exc. handling
            VisualizeRecursively(null, input, "");

            //close off structs and graph
            _nodeBuilder.AppendLine();
            _nodeBuilder.Replace(System.Environment.NewLine, " } \" ]" + System.Environment.NewLine);
            streamWriter.WriteLine(_nodeBuilder);
            streamWriter.Write(_edgeBuilder);
            streamWriter.WriteLine("}");

            //flush streamWriter and close file
            streamWriter.Flush();
            streamWriter.Close();

        }

        /// <summary>
        /// Inspect all properties and fields in object 'input' and calls this method again for every
        /// member. Creates nodes and edges in the DOT file for visualization. Whitelist, Blacklist and 
        /// ToString() implementation of members determines process by ignoring values except for those 
        /// named in Whitelist while skipping members named in Blacklist.
        /// </summary>
        /// <param name="input">object to which the member belongs</param>
        /// <param name="component">member to be handled in this method call</param>
        /// <param name="source">contains the source node and specific struct item (123:3) as string</param>"
        /// <param name="destId">current id to identify nodes for visualization</param>
        private static void VisualizeRecursively(dynamic input, dynamic component, string source)
        {

            //the actual object if behind a property/field
            dynamic memberObj = null;

            //handling the first object not behind a member var
            if (input == null) memberObj = component;
            else memberObj = component.GetValue(input);

            //object has been processed and struct drawn already
            if (_processedNodes.TryGetValue(memberObj, out string processedNode))
            {
                _edgeBuilder.AppendLine("struct"
                                       + source
                                       + " -> "
                                       + "struct"
                                       + processedNode);
                return;
            }

            //save all recursive function calls and invoke later to prevent
            //overriding wrong line
            var recursiveCalls = new List<Action>();

            //get new Id for new struct
            uint currId = GetId();

            //inner Id for sections of struct
            uint innerId = 0;

            //create a node for the object behind the member
            if (input != null) _nodeBuilder.AppendLine();
            _nodeBuilder.Append("struct"
                               + currId
                               + " [shape=record label=\" { "
                               + memberObj.GetType().Name
                               + " ");

            if (input != null)
            {
                _edgeBuilder.AppendLine("struct"
                                       + source
                                       + " -> "
                                       + "struct"
                                       + currId);
            }



            IterateMembers(memberObj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            IterateMembers(memberObj.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            void IterateMembers(IEnumerable<MemberInfo> unspecificMembers)
            {
                foreach (var unspecificMember in unspecificMembers)
                {
                    dynamic member;
                    if (unspecificMember as PropertyInfo != null)
                        member = (PropertyInfo) unspecificMember;

                    else if (unspecificMember as FieldInfo != null)
                        member = (FieldInfo) unspecificMember;

                    else continue;

                    //needed because of nested loop
                    bool skipMember = false;
                    foreach (var entry in _blacklist)
                    {
                        if (!_whitelist.Contains(member.Name) && member.Name.Contains(entry))
                        {
                            skipMember = true;
                            break;
                        }
                    }

                    if (skipMember) continue;

                    innerId++;
                    //to draw the edge from later on
                    string destination = new string(currId + ":" + innerId);

                    //object behind member is null, create entry but don't follow it up
                    if (member.GetValue(memberObj) == null)
                    {
                        _nodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + member.Name
                                           + " (∅) ");

                    }
                    else if (member.GetValue(memberObj).GetType().IsValueType)
                    {

                        if (_whitelist.Contains(member.Name))
                        {
                            _nodeBuilder.Append("| <"
                                               + innerId
                                               + "> "
                                               + member.Name
                                               + ": "
                                               + member.GetValue(memberObj)
                                               + " ");
                        }
                        else
                        {
                            _nodeBuilder.Append("| <"
                                               + innerId
                                               + "> "
                                               + member.Name
                                               + " ");
                        }

                    }
                    //reference type
                    else
                    {
                        _nodeBuilder.Append("| <"
                                           + innerId
                                           + "> "
                                           + member.Name
                                           + " ");

                        recursiveCalls.Add(new Action(() =>
                            VisualizeRecursively(memberObj, member, destination)));
                    }
                }
            }

            foreach (var call in recursiveCalls)
            {
                call.Invoke();
            }

        }

    }
}
