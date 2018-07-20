using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CovarianceVaRCalculator_Base;

namespace CovarianceVaRCalculator_Engine
{
	internal class Matrix
	{
		public readonly int numRows;
		public readonly int numColumns;
		private readonly double[][] _elements;

		// Creates a new matrix, numRows x numColumns, with all elements equal to zero.
		public Matrix(int nRows, int nColumns)
		{
			try
			{
				numRows = nRows;
				numColumns = nColumns;
				_elements = new double[numRows][];
				for (int r = 0; r < numRows; r++)
					_elements[r] = new double[numColumns];
			}
			catch (OutOfMemoryException e)
			{
				throw new CovarianceVaRCalculator_Exception(String.Format("Memory allocation failed! Message: {0}", e.Message), e);
			}
		}

		// Returns element [r,c]
		public double this[int r, int c]
		{
			get { return _elements[r][c]; }
			set { _elements[r][c] = value; }
		}

		public double[,] ToArray()
		{
			try
			{
				double[,] array = new double[numRows, numColumns];
				for (int r = 0; r < numRows; r++)
					for (int c = 0; c < numColumns; c++)
						array[r, c] = _elements[r][c];
				return array;
			}
			catch (OutOfMemoryException e)
			{
				throw new CovarianceVaRCalculator_Exception(String.Format("Memory allocation failed! Message: {0}", e.Message), e);
			}
		}

		// Matrix addition
		public Matrix Add(Matrix M)
		{
			if (numRows != M.numRows || numColumns != M.numColumns)
				throw new CovarianceVaRCalculator_Exception("Cannot add matrices; they must be the same size.");
			Matrix sum = new Matrix(numRows, numColumns);
			for (int r = 0; r < numRows; r++)
				for (int c = 0; c < numColumns; c++)
					sum._elements[r][c] = _elements[r][c] + M._elements[r][c];
			return sum;
		}

		// Matrix subtraction
		public Matrix Subtract(Matrix M)
		{
			if (numRows != M.numRows || numColumns != M.numColumns)
				throw new CovarianceVaRCalculator_Exception("Cannot subtract matrices; they must be the same size.");
			Matrix S = new Matrix(numRows, numColumns);
			for (int r = 0; r < numRows; r++)
				for (int c = 0; c < numColumns; c++)
					S._elements[r][c] = _elements[r][c] - M._elements[r][c];
			return S;
		}

		// Matrix transpose
		public Matrix Transpose()
		{
			try
			{
				Matrix T = new Matrix(numColumns, numRows);
				for (int r = 0; r < numRows; r++)
					for (int c = 0; c < numColumns; c++)
						T._elements[c][r] = _elements[r][c];
				return T;
			}
			catch (OutOfMemoryException e)
			{
				throw new CovarianceVaRCalculator_Exception(String.Format("Memory allocation failed! Message: {0}", e.Message), e);
			}
		}

		// Matrix multiplication
		public Matrix Multiply(Matrix M)
		{
			if (numColumns != M.numRows)
				throw new CovarianceVaRCalculator_Exception("Cannot multiply matrices; number of columns in first matrix must equal the number of rows in the second.");
			try
			{
				Matrix product = new Matrix(numRows, M.numColumns);
				for (int r = 0; r < product.numRows; r++)
					for (int c = 0; c < product.numColumns; c++)
					{
						double d = 0;
						for (int i = 0; i < numColumns; i++)
							d += _elements[r][i] * M._elements[i][c];
						product._elements[r][c] = d;
					}
				return product;
			}
			catch (OutOfMemoryException e)
			{
				throw new CovarianceVaRCalculator_Exception(String.Format("Memory allocation failed! Message: {0}", e.Message), e);
			}
		}
	}
}
