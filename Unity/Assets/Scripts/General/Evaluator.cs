using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

class Node
{
	public int operation;
	public double value;
	public Node left;
	public Node right;
	public Node() // default constructor, no data
	{
		operation = 0;
		value = 0;
	}
	public Node(int operation) //operation node
	{
		this.operation = operation;
		value = 0;
	}
	public Node(double value) //value node
	{
		operation = 0;
		this.value = value;
	}
}
/* operations:
 * 0 - number type, no operation
 * 1 +
 * 2 -
 * 3 *
 * 4 /
 * 5 ^ 
 * 6 %
 * 7 ==
 * 8 !=
 * 9 <
 * 10 >
 * 11 <=
 * 12 >=
 * 13 &&
 * 14 ||
 * 15 !  - this type of node will only have a left side, to be evaluated as not
 */

public class Evaluator
{
	private int position;
	private readonly string expression;
	public Evaluator(string expression)
	{
		this.expression = expression;
		position = 0;
	}
	public int Evaluate(string input)
	{
		Node tree = Parse();
	}
	private Node Parse()
	{
		char token = expression[position];
		position++;
		if (position >= expression.Length)
		{
			return null;
		}

		if (char.IsDigit(token)) // rule 1 - if token is string 
		{
			char d = token;
			string accum = token.ToSafeString();
			while ((char.IsDigit(d) || d == '.') && position < expression.Length)
			{
				accum += expression[position];
				position++;
			}
			double value = double.Parse(accum);
			return new Node(value);
		}
		else if (token == '^') {
			Node node = new(5){
				left = Parse()
			};
			return node;
		}
		else if (token == '(') {
			Node node = new(){
				left = Parse()
			};
			return node;
		}
		else if(token == '*')
		{
			Node node = new(3){
				right = Parse()
			};
			return node;
		}
		else if (token == '/')
		{
			Node node = new(4){
				right = Parse()
			};
			return node;
		}
		else if (token == '+')
		{
			Node node = new(3)
			{
				left = Parse()
			};
			return node;
		}

	}
}
