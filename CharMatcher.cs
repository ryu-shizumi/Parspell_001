﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OPC
{
    public abstract class CharMatcher : Matcher
    {

        public NotCharMatcher Not
        {
            get { return new NotCharMatcher(this); }
        }

        public static AnyCharMatcher operator |(CharMatcher a, CharMatcher b)
        {
            return new AnyCharMatcher(a, b);
        }
        public static AnyCharMatcher operator |(CharMatcher a, char b)
        {
            return a | b._();
        }
    }

    /// <summary>
    /// 文字１個と照合するマッチャー
    /// </summary>
    public class SimpleCharMatcher : CharMatcher
    {
        private CharRange _charRange;


        public SimpleCharMatcher(char c)
        {
            _charRange = new CharRange(c, c);
        }
        public SimpleCharMatcher(char min, char max)
        {
            _charRange = new CharRange(min, max);
        }
        public SimpleCharMatcher(CharRange charRange)
        {
            _charRange = charRange;
        }

        private SimpleCharMatcher(CharRange charRange, string name)
        {
            _charRange = charRange;
            Name = name;
        }

        public override Match Match(TokenList tokenList, int tokenIndex, string nest)
        {
            // マッチリストにある時はそれを返す
            if (_matchList.ContainsKey(tokenIndex, this)) { return _matchList[tokenIndex, this]; }

            Match result;
            var token = tokenList[tokenIndex];
            // 現在位置が文字トークンの時
            if (token is TokenChar cToken)
            {
                // 範囲が合致した時
                if (_charRange.IsMatch(cToken.Char))
                {
                    // 正解マッチを作成する
                    result = new Match(this, tokenIndex, 1);
                    _matchList[tokenIndex, this] = result;
                    return result;
                }
            }

            // 失敗マッチを作成する
            result = new FailMatch(this, tokenIndex,1);
            _matchList[tokenIndex, this] = result;
            return result;
        }

        

        
        public override void DebugOut(HashSet<RecursionMatcher> matchers, string nest)
        {
            var sb = new StringBuilder();

            //if ((_charRanges.Count == 1) && (_charRanges[0].Min == _charRanges[0].Max))
            //{
            //    sb.Append('"');
            //    sb.Append(_charRanges[0].Min.ToString());
            //    sb.Append('"');
            //}
            //else
            //{
            //    sb.Append("[");
            //    foreach (var range in _charRanges)
            //    {
            //        if (range.Min == range.Max)
            //        {
            //            sb.Append(range.Min);
            //        }
            //        else
            //        {
            //            sb.Append($"{range.Min}-{range.Max}");
            //        }
            //    }
            //    sb.Append("]");
            //}

            Debug.WriteLine($"{nest} {sb.ToString()}");
        }

        public SimpleCharMatcher this[string Name]
        {
            get
            {
                return new SimpleCharMatcher(_charRange, Name);
            }
        }
    }

    /// <summary>
    /// 選択マッチャー
    /// </summary>
    public class AnyCharMatcher : CharMatcher
    {
        private CharMatcher[] _inners;

        public AnyCharMatcher(CharMatcher left, CharMatcher right)
        {
            List<CharMatcher> list = new List<CharMatcher>();

            if(left is AnyCharMatcher anyLeft)
            {
                list.AddRange(anyLeft._inners);
            }
            else
            {
                list.Add(left);
            }

            if (right is AnyCharMatcher anyRight)
            {
                list.AddRange(anyRight._inners);
            }
            else
            {
                list.Add(right);
            }

            _inners = list.ToArray();
        }
        public AnyCharMatcher(CharMatcher[] left, CharMatcher[] right)
        {
            List<CharMatcher> list = new ();

            list.AddRange(left);
            list.AddRange(right);

            _inners = list.ToArray();
        }
        public AnyCharMatcher(CharMatcher[] left, CharMatcher right)
        {
            List<CharMatcher> list = new();

            list.AddRange(left);
            list.Add(right);

            _inners = list.ToArray();
        }
        public AnyCharMatcher(CharMatcher left, CharMatcher[] right)
        {
            List<CharMatcher> list = new();

            list.Add(left);
            list.AddRange(right);

            _inners = list.ToArray();

        }
        private AnyCharMatcher(CharMatcher[] inners, string name)
        {
            _inners = inners;
            Name = name;
        }

        public override Match Match(TokenList tokenList, int tokenIndex, string nest)
        {
            // if (DebugName != "") { Debug.WriteLine(nest + DebugName + "[" + tokenIndex.ToString() + "]"); }

            // マッチリストにある時はそれを返す
            if (_matchList.ContainsKey(tokenIndex, this)) { return _matchList[tokenIndex, this]; }

            Match result;

            int currentIndex = tokenIndex;
            Match tempResult;

            foreach (var inner in _inners)
            {
                tempResult = inner.Match(tokenList, currentIndex, nest + "  ");
                if (tempResult.IsSuccess)
                {
                    result = tempResult;// new WrapMatch(this, tempResult);
                    if(Name != "")
                    {
                        result = tempResult[Name];
                    }
                    _matchList[tokenIndex, this] = result;
                    return result;
                }
            }

            result = new FailMatch(this, tokenIndex);
            _matchList[tokenIndex, this] = result;
            return result;
        }

        public static AnyCharMatcher operator |(AnyCharMatcher a, AnyCharMatcher b)
        {
            return new AnyCharMatcher(a._inners, b._inners);
        }
        public static AnyCharMatcher operator |(AnyCharMatcher a, CharMatcher b)
        {
            return new AnyCharMatcher(a._inners, b);
        }

        public override void DebugOut(HashSet<RecursionMatcher> matchers, string nest)
        {
            foreach (var inner in _inners)
            {
                inner.DebugOut(matchers, nest + "  ");
            }
        }

        public AnyCharMatcher this[string Name]
        {
            get
            {
                return new AnyCharMatcher(_inners, Name);
            }
        }
    }

    /// <summary>
    /// 文字１個の否定と照合するマッチャー
    /// </summary>
    public class NotCharMatcher : Matcher
    {
        public CharMatcher Inner { get; private set; }

        public NotCharMatcher(CharMatcher inner)
        {
            Inner = inner;
        }
        private NotCharMatcher(CharMatcher inner, string name)
        {
            Inner = inner;
            Name = name;
        }

        public NotCharMatcher this[string Name]
        {
            get
            {
                return new NotCharMatcher(Inner, Name);
            }
        }

        public override Match Match(TokenList tokenList, int tokenIndex, string nest)
        {
            var innerResult = Inner.Match(tokenList, tokenIndex, nest);

            if(innerResult.IsSuccess)
            {
                return new FailMatch(this, innerResult.TokenIndex);
            }

            return new WrapMatch(this,innerResult);
        }

        public override void DebugOut(HashSet<RecursionMatcher> matchers, string nest)
        {
            throw new NotImplementedException();
        }
    }
}