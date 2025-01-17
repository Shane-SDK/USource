using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USource
{
    public class DependencyTree
    {
        public DependencyTree Root
        {
            get
            {
                DependencyTree parent = this;
                while(parent.parent != null)
                {
                    parent = parent.parent;
                }
                return parent;
            }
        }
        public DependencyTree(Location location, DependencyTree parent = null)
        {
            this.location = location;
            children = new();
            this.parent = parent;
        }

        public IEnumerable<Location> GetImmediateChildren(bool includeSelf = true)
        {
            foreach (var child in children.Where(e => (includeSelf || e.location.SourcePath != location.SourcePath)))
            {
                yield return child.location;
            }
        }
        public IEnumerable<Location> RecursiveChildren
        {
            get
            {
                Queue<DependencyTree> queue = new();
                queue.Enqueue(this);

                while (queue.Count > 0)
                {
                    var tree = queue.Dequeue();

                    foreach (DependencyTree child in tree.children)
                        queue.Enqueue(child);

                    yield return tree.location;
                }
            }
        }
        public Location location;
        List<DependencyTree> children;
        public DependencyTree parent;
        public void Add(Location location)
        {
            var child = new DependencyTree(location, this);
            children.Add(child);
        }
    }
}
