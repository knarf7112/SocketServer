using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Func.Test
{
    //ref:https://msdn.microsoft.com/en-us/library/ms379572(v=vs.80).aspx
    class BinaryTreeStructure
    {
    }

    class Node<T>
    {
        private T data;
        private NodeList<T> neighbors = null;

        public Node() { }

        public Node(T data) : this(data,null) {  }

        public Node(T data, NodeList<T> neighbors) { this.data = data; this.neighbors = neighbors; }


        public T Value
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
            }
        }

        public NodeList<T> Neighbors 
        {
            get
            {
                return this.neighbors;
            }
            set
            {
                this.neighbors = value;
            }
        }
    }

    class NodeList<T> : Collection<Node<T>>
    {
        public NodeList(): base() { }

        public NodeList(int initialSize)
        {
            // Add the specified number of items
            for (int i = 0; i < initialSize; i++)
                base.Items.Add(default(Node<T>));
        }

        public Node<T> FindByValue(T value)
        {
            // search the list for the value
            foreach (var item in base.Items)
            {
                if (value.Equals(item.Value))
                {
                    return item;
                }
            }
            // if we reached here, we didn't find a matching node
            return null;
        }
    }

    class BinaryTreeNode<T> : Node<T>
    {
        public BinaryTreeNode() { }
        public BinaryTreeNode(T data) : base(data, null) { }
        public BinaryTreeNode(T data, BinaryTreeNode<T> left, BinaryTreeNode<T> right) 
        {
            this.Value = data;

            NodeList<T> children = new NodeList<T>(2);
            children[0] = left;
            children[1] = right;

            base.Neighbors = children;
        }
        //二元樹的左邊
        public BinaryTreeNode<T> Left
        {
            get
            {
                if (base.Neighbors != null)
                {
                    return (BinaryTreeNode<T>)base.Neighbors[0];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (base.Neighbors == null)
                    base.Neighbors = new NodeList<T>(2);
                base.Neighbors[0] = value;
            }
        }
        //二元樹的右邊
        public BinaryTreeNode<T> Right
        {
            get
            {
                if (base.Neighbors == null)
                {
                    return null;
                }
                else
                {
                    return (BinaryTreeNode<T>)base.Neighbors[1];
                }
            }
            set
            {
                if (base.Neighbors == null)
                    base.Neighbors = new NodeList<T>(2);
                base.Neighbors[1] = value;
            }
        }

    }

    class BinaryTree<T>
    {
        private BinaryTreeNode<T> root;
        public BinaryTree() { root = null; }

        public virtual void Clear()
        {
            root = null;
        }

        public BinaryTreeNode<T> Root
        {
            get { return root; }
            set { this.root = value; }
        }
    }
}
