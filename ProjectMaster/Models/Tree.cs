using System;
using System.Collections.Generic;

namespace ProjectMaster.Models
{
    public class Tree
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public TreeType Type { get; private set; }

        public Tree[] Children { get; private set; }

        public enum TreeType
        {
            File = 1,
            Folder = 2,
        }

        protected Tree(string name, string path, TreeType type)
        {
            this.Name = name;
            this.Path = path;
            this.Type = type;
        }

        public class Generator
        {
            protected Generator()
            {
                this._tree = new Tree(null, null, 0);
            }

            private readonly Tree _tree;

            public Generator SetName(string name)
            {
                _tree.Name = name;
                return this;
            }

            public Generator SetPath(string path)
            {
                _tree.Path = path;
                return this;
            }

            public Generator SetType(TreeType type)
            {
                _tree.Type = type;
                return this;
            }

            public Generator SetChildren(Tree[] children)
            {
                _tree.Children = children;
                return this;
            }

            public static Tree[] Create<TRoot>(IEnumerable<TRoot> root, Func<TRoot, string> getName, Func<TRoot, string> getPath, Func<TRoot, TreeType> getType, Func<TRoot, IEnumerable<TRoot>> childSelector)
                => Create<TRoot>(root.GetEnumerator(), getName, getPath, getType, t => childSelector(t).GetEnumerator());

            public static Tree[] Create<TRoot>(IEnumerator<TRoot> root, Func<TRoot, string> getName, Func<TRoot, string> getPath, Func<TRoot, TreeType> getType, Func<TRoot, IEnumerable<TRoot>> childSelector)
                => Create<TRoot>(root, getName, getPath, getType, t => childSelector(t).GetEnumerator());

            public static Tree[] Create<TRoot>(IEnumerable<TRoot> root, Func<TRoot, string> getName, Func<TRoot, string> getPath, Func<TRoot, TreeType> getType, Func<TRoot, IEnumerator<TRoot>> childSelector)
                => Create<TRoot>(root.GetEnumerator(), getName, getPath, getType, childSelector);

            public static Tree[] Create<TRoot>(IEnumerator<TRoot> root, Func<TRoot, string> getName, Func<TRoot, string> getPath, Func<TRoot, TreeType> getType, Func<TRoot, IEnumerator<TRoot>> childSelector)
            {
                var list = new List<Tree>();
                while (root.MoveNext())
                {
                    var generator = new Generator();
                    var current = root.Current;

                    var treeType = getType(current);
                    if (treeType == 0)
                        continue;
                    generator.SetName(getName(current));
                    generator.SetPath(getPath(current));
                    generator.SetType(treeType);

                    if (treeType == TreeType.Folder)
                    {
                        var childEnumerator = childSelector(current);

                        var childTrees = Create(childEnumerator, getName, getPath, getType, childSelector);

                        generator.SetChildren(childTrees);
                    }

                    list.Add(generator.Tree);
                }

                return list.ToArray();
            }

            private Tree Tree => _tree;
        }
    }
}