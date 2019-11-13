using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataStructureVisualization
{
    /// <summary>
    /// DataStructureVisualizer creates a DOT file containing graph description language
    /// which can be visualized with graphviz (graphviz.org). Visualization can be influenced by
    /// optional arguments showing or hiding various members.
    /// ----------------------------
    /// ###Created by David Kastner
    /// </summary>
    static class DataStructureVisualizer
    {
        //contains member names referencing duplicate information etc
        private static readonly List<string> DefaultBlacklist = new List<string>()
        {
            "k__BackingField",
            "m_value",
            "_firstChar",
            "Item",
            "IsFixedSize",
            "IsReadOnly",
            "Capacity",
            "Count",
            "IsSynchronized",
            "SyncRoot",
            "_array",
            "_items",
            "_version",
            "_size",
            "_head",
            "_tail",
            "Comparer",
            "First",
            "Last",
            "Keys",
            "Values",
            "length",
            "LongLength",
            "Rank"
        };

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
        /// currently not used in this version as of 19/10/2019
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

        public static IEnumerable GetDefaultBlacklist()
        {
            return DefaultBlacklist;
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

            _internalNodeId = 0;
            _processedNodes = new Dictionary<object, string>();

            _whitelist = new List<string>();

            //standard properties and fields in data structures from package
            //Collections to avoid obfuscating the relevant information
            //add names to whitelist to show specific ones hidden by default 
            _blacklist = new List<string>(DefaultBlacklist);

            _nodeBuilder = new StringBuilder();
            _edgeBuilder = new StringBuilder();

            if (whitelistedMembers != null)
                foreach (var x in whitelistedMembers)
                    _whitelist.Add(x);

            if (blacklistedMembers != null)
                foreach (var x in blacklistedMembers)
                    _blacklist.Add(x);

            var streamWriter = new StreamWriter("vis_" + input.GetType().Name + ".dot");

            streamWriter.WriteLine(
                "//created " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " by DataStructureVisualizer (Kastner David)");

            streamWriter.Write($"digraph DataStructureVisualization {{");

            //graphviz graph style options
            streamWriter.Write($"\n\n  graph [" +
                               $"\n    labelloc = t" +
                               $"\n    ranksep = 0.3 " +
                               $"\n    nodesep = 0.4 " +
                               $"\n    rankdir = TB" +
                               $"\n    style = \"dotted, filled\"" +
                               $"\n    fillcolor = \"#FFFFDF\"" +
                               $"\n  ]");

            //graphviz node style options
            streamWriter.Write($"\n  node [" +
                               $"\n    colorscheme = \"pastel17\"" +
                               $"\n    style = \"filled\"" +
                               $"\n    fillcolor = 2" +
                               $"\n    shape = record" +
                               $"\n  ]");

            //legend for user information
            streamWriter.Write($"\n  subgraph cluster_legend {{" +
                               $"\n    label = \"Legend\"" +
                               $"\n    ranksep = 0.5" +
                               $"\n    nodesep = record" +
                               $"\n    labelloc = t" +
                               $"\n    l1 [label=\"IEnumerable\", fillcolor=5]" +
                               $"\n    l2 [label=\"ValueType\", fillcolor=3]" +
                               $"\n    l3 [label=\"Object\", fillcolor=2]" +
                               $"\n  }}");

            //replace symbols for graphviz syntax
            var graphName = Regex.Replace(Regex.Replace(input.GetType().Name.ToString(),
                "`", ""),
                "\\[\\]", "");

            //creating subgraph for the input object
            streamWriter.Write($"\n\nsubgraph {graphName} {{\n");


            try
            {
                VisualizeRecursively(null, input, "");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Thrown exc.: {e.GetType().Name} with msg:");
                Console.WriteLine($"{e.Message}");
            }
            finally
            {
                //close off structs and graph
                _nodeBuilder.AppendLine();
                _nodeBuilder.Replace(System.Environment.NewLine, " } \" ]" + System.Environment.NewLine);
                streamWriter.WriteLine(_nodeBuilder);
                streamWriter.Write($"{_edgeBuilder}}}\n}}");

                //flush streamWriter and close file
                streamWriter.Flush();
                streamWriter.Close();
            }

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

            //save all recursive function calls and invoke later to prevent overriding wrong line
            var recursiveCalls = new List<Action>();

            //get new Id for new struct
            uint currId = GetId();

            //inner Id for sections of struct
            uint innerId = 0;

            //add to processed nodes to avoid cycles
            _processedNodes.Add(memberObj, currId + ":" + innerId);

            if (memberObj is IEnumerable)
            {
                if (source != "")
                {
                    _nodeBuilder.AppendLine();
                    _edgeBuilder.AppendLine("struct"
                                            + source
                                            + " -> "
                                            + "struct"
                                            + currId
                                            + ":"
                                            + innerId);
                }


                source = "0:0";
                innerId++;

                if (memberObj is string)
                {
                    _nodeBuilder.Append("struct"
                                        + currId
                                        + " [shape=record"
                                        + " fillcolor=5"
                                        + " label=\" { "
                                        + "<0>"
                                        + memberObj.GetType().Name
                                        + " | <"
                                        + innerId
                                        + "> "
                                        + memberObj
                                        + " ");

                }
                else if (input != null && !_whitelist.Contains(component.Name))
                {
                    _nodeBuilder.Append("struct"
                                        + currId
                                        + " [shape=record"
                                        + " fillcolor=5"
                                        + " label=\" { "
                                        + memberObj.GetType().Name
                                        + " | <"
                                        + innerId
                                        + ">"
                                        + "..."
                                        + " ");

                }
                else
                {
                    //check if IEnumerable is empty
                    var enumerator = ((IEnumerable)memberObj).GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        _nodeBuilder.Append("struct"
                                            + currId
                                            + " [shape=record "
                                            + " fillcolor=5 "
                                            + "label=\" { "
                                            + "<"
                                            + innerId
                                            + ">"
                                            + memberObj.GetType().Name
                                            + " ");
                    }
                    else
                    {
                        _nodeBuilder.Append("struct"
                                            + currId
                                            + " [shape=record"
                                            + " fillcolor=5"
                                            + " label=\" { "
                                            + "<"
                                            + innerId
                                            + ">"
                                            + memberObj.GetType().Name
                                            + "\\{\\}");

                        return;

                    }

                    string collectionSource = currId + ":" + innerId;


                    IterateMembers(memberObj, memberObj.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

                    IterateMembers(memberObj, memberObj.GetType()
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

                    if (memberObj.GetType().isArray)
                    {
                        if (memberObj.Count == 0) return;
                        currId = GetId();
                        innerId = 0;
                        _nodeBuilder.AppendLine();
                        _nodeBuilder.Append("struct"
                                            + currId
                                            + " [shape=record"
                                            + " fillcolor=3"
                                            + " label=\" { ");

                        foreach (var entry in memberObj)
                        {
                            if //append every indexs here
                            _nodeBuilder.Append("")


                            //somethign like this
                                recursiveCalls.Add(new Action(() =>
                                    VisualizeRecursively(null, entry, collectionSource)));

                        }


                    }
                    else //is list or some sort of other enumerable
                    {

                        foreach (var entry in memberObj)
                        {
                            if (entry == null) continue;
                            currId = GetId();
                            innerId = 0;

                            if (entry.GetType().IsValueType)
                            {
                                _nodeBuilder.AppendLine();
                                _nodeBuilder.Append("struct"
                                                    + currId
                                                    + " [shape=record"
                                                    + " fillcolor=3"
                                                    + " label=\" { "
                                                    + "<"
                                                    + innerId
                                                    + ">"
                                                    + entry
                                                    + " ");

                                _edgeBuilder.AppendLine("struct"
                                                        + source
                                                        + " -> "
                                                        + "struct"
                                                        + currId
                                                        + ":"
                                                        + innerId);
                            }
                            else
                            {

                                recursiveCalls.Add(new Action(() =>
                                    VisualizeRecursively(null, entry, collectionSource)));

                            }
                        }
                    }
                }
            }
            else
            {
                if (currId > 0) _nodeBuilder.AppendLine();
                _nodeBuilder.Append("struct"
                                    + currId
                                    + " [shape=record label=\" { "
                                    + "<0>"
                                    + memberObj.GetType().Name
                                    + " ");



                //create an edge coming from it's reference variable
                if (source != "")
                {
                    _edgeBuilder.AppendLine("struct"
                                            + source
                                            + " -> "
                                            + "struct"
                                            + currId
                                            + ":0 ");
                }


                //get all public/private/... member and handle them
                IterateMembers(memberObj, memberObj.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

                IterateMembers(memberObj, memberObj.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            }

            //Properties and fields have the same methods, generalized in this local function
            void IterateMembers(dynamic sourceObject, IEnumerable<MemberInfo> unspecificMembers)
            {
                foreach (var unspecificMember in unspecificMembers)
                {
                    dynamic member;
                    if (unspecificMember as PropertyInfo != null)
                        member = (PropertyInfo)unspecificMember;

                    else if (unspecificMember as FieldInfo != null)
                        member = (FieldInfo)unspecificMember;

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

                    if (member.GetValue(sourceObject) is IEnumerable)
                    {
                        _nodeBuilder.Append("| <"
                                            + innerId
                                            + "> "
                                            + member.Name
                                            + " ");


                        //if (member.GetValue(sourceObject) is string)
                        {
                            recursiveCalls.Add(new Action(() =>
                                VisualizeRecursively(null, member.GetValue(sourceObject), destination)));
                        }


                    }
                    //object behind member is null, create entry but don't follow it up
                    else if (member.GetValue(sourceObject) == null)
                    {
                        _nodeBuilder.Append("| <"
                                            + innerId
                                            + "> "
                                            + member.Name
                                            + " (∅) ");

                    }
                    else if (member.GetValue(sourceObject).GetType().IsValueType)
                    {

                        if (_whitelist.Contains(member.Name))
                        {
                            _nodeBuilder.Append("| <"
                                                + innerId
                                                + "> "
                                                + member.Name
                                                + ": "
                                                + member.GetValue(sourceObject)
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
                            VisualizeRecursively(sourceObject, member, destination)));
                    }
                }
            }

            //invoke all recursive calls for each reference typed member
            foreach (var call in recursiveCalls) call.Invoke();

        }
    }

}
