using System;
using System.Collections.Generic;
using System.Linq;

namespace VMFParser
{
    public static class Utils
    {
        public enum Mode
        {
            KeyValue,
            BlockName,
            Formatting
        }
        public static Type GetNodeType(string line)
        {
            return line.Trim().StartsWith("\"") ? typeof(VProperty) : typeof(VBlock);
        }

        public static IList<IVNode> ParseToBody(string[] body)
        {
            IList<IVNode> newBody = new List<IVNode>();
            int depth = 0;
            var wasDeep = false;
            IList<string> nextBlock = null;
            for (int i = 0; i < body.Length; i++)
            {
                var line = body[i].Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                var readable = line.FirstOrDefault() != default(char);

                if (readable && line.First() == '{')
                    depth++;

                if (depth == 0)
                    if (Utils.GetNodeType(line) == typeof(VProperty))
                        newBody.Add(new VProperty(line));
                    else
                    {
                        nextBlock = new List<string>();
                        nextBlock.Add(line);
                    }
                else
                    nextBlock.Add(line);

                wasDeep = depth > 0;

                if (readable && line.First() == '}')
                    depth--;

                if (wasDeep && depth == 0)
                {
                    newBody.Add(new VBlock(nextBlock.ToArray()));
                    nextBlock = null;
                }
            }
            return newBody;
        }
        public static IList<IVNode> ParseToBody(string data)
        {
            IList<IVNode> newBody = new List<IVNode>();

            string name = "";
            int propertyCounter = 0;
            Mode mode = Mode.BlockName;
            List<IVNode> nodeStack = new List<IVNode>();

            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];

                if (mode == Mode.KeyValue || mode == Mode.BlockName)
                {
                    // determine whether to escape
                    if (mode == Mode.BlockName && (char.IsWhiteSpace(c)))
                    {
                        // Terminate from block
                        // Push new block node to stack
                        VBlock block = new VBlock(name);
                        nodeStack.Add(block);
                        mode = Mode.Formatting;
                        continue;
                    }
                    if (mode == Mode.KeyValue && (c == '"'))
                    {
                        // Terminate
                        bool isKey = propertyCounter % 2 == 0;
                        if (string.IsNullOrEmpty(name))  // If the beginning
                        {
                            
                        }
                        else  // End name
                        {
                            if (isKey)
                            {
                                nodeStack.Add(new VProperty(name));
                            }
                            else
                            {
                                (nodeStack[^1] as VProperty).Value = name;

                            }

                            propertyCounter++;
                            name = "";
                            mode = Mode.Formatting;
                        }

                        continue;
                    }

                    name += c;
                }
                else if (mode == Mode.Formatting)
                {
                    if (c == '}')
                    {
                        // Collapse nodestack
                        // Take all newest non block nodes and place them into the newest block node

                        int blockIndex = -1;
                        for (int n = nodeStack.Count - 1; n >= 0; n--)
                        {
                            IVNode currentNode = nodeStack[n];
                            if (currentNode is VBlock)
                            {
                                blockIndex = n;
                                break;
                            }
                        }

                        VBlock block = nodeStack[blockIndex] as VBlock;
                        for (int n = blockIndex + 1; n < nodeStack.Count; n++)
                        {
                            block.Body.Add(nodeStack[n]);
                        }

                        nodeStack.RemoveRange(blockIndex + 1, nodeStack.Count - (blockIndex + 1));
                    }
                }
            }

            return newBody;
        }

        public static IList<string> BodyToString(IList<IVNode> body)
        {
            IList<string> text = new List<string>();
            if (body != null)
                foreach (var node in body)
                    if (node.GetType() == typeof(VProperty))
                        text.Add(((VProperty)node).ToVMFString());
                    else
                        foreach (string s in ((VBlock)node).ToVMFStrings())
                            text.Add(s);
            return text;
        }
    }
}
