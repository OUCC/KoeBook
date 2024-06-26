﻿using KoeBook.Epub.Services;

namespace KoeBook.Test.Epub;

public class SplitBraceServiceTest
{
    public static object[][] TestCases()
    {
        (string, List<string>)[] cases = [
            // '「''」'のみの場合のケース
            ("「", ["「"]),
            ("」", ["」"]),
            ("a", ["a"]),
            ("abc「abc」abc", ["abc", "「abc」", "abc"]),
            ("abc「abc」", ["abc", "「abc」"]),
            ("「abc」abc", ["「abc」", "abc",]),
            ("abc「abc」", ["abc", "「abc」"]),
            ("「abc」", ["「abc」",]),
            ("abc「abc", ["abc", "「abc"]),
            ("abc「", ["abc", "「"]),
            ("「abc", ["「abc"]),
            ("abc」abc", ["abc」", "abc"]),
            ("abc」", ["abc」"]),
            ("」abc", ["」", "abc"]),
            ("abc「abc」abc「abc」abc", ["abc", "「abc」", "abc", "「abc」", "abc"]),
            ("「abc」abc「abc」abc", ["「abc」", "abc", "「abc」", "abc"]),
            ("abc「abc」「abc」abc", ["abc", "「abc」", "「abc」", "abc"]),
            ("abc「abc」abc「abc」", ["abc", "「abc」", "abc", "「abc」"]),
            ("abc「abc「abc」abc」abc", ["abc", "「abc「abc」abc」", "abc"]),
            ("abc「abc「abc」abc", ["abc", "「abc「abc」abc"]),
            ("abc「abc」abc」abc", ["abc「abc」abc」", "abc"]),
            ("abc「abc「abc", ["abc", "「abc「abc"]),
            ("abc」abc」abc", ["abc」abc」", "abc"]),
            // '『''』'のみの場合のケース
            ("『", ["『"]),
            ("』", ["』"]),
            ("a", ["a"]),
            ("abc『abc』abc", ["abc", "『abc』", "abc"]),
            ("abc『abc』", ["abc", "『abc』"]),
            ("『abc』abc", ["『abc』", "abc",]),
            ("abc『abc』", ["abc", "『abc』"]),
            ("『abc』", ["『abc』",]),
            ("abc『abc", ["abc", "『abc"]),
            ("abc『", ["abc", "『"]),
            ("『abc", ["『abc"]),
            ("abc』abc", ["abc』", "abc"]),
            ("abc』", ["abc』"]),
            ("』abc", ["』", "abc"]),
            ("abc『abc』abc『abc』abc", ["abc", "『abc』", "abc", "『abc』", "abc"]),
            ("『abc』abc『abc』abc", ["『abc』", "abc", "『abc』", "abc"]),
            ("abc『abc』『abc』abc", ["abc", "『abc』", "『abc』", "abc"]),
            ("abc『abc』abc『abc』", ["abc", "『abc』", "abc", "『abc』"]),
            ("abc『abc『abc』abc』abc", ["abc", "『abc『abc』abc』", "abc"]),
            ("abc『abc『abc』abc", ["abc", "『abc『abc』abc"]),
            ("abc『abc』abc』abc", ["abc『abc』abc』", "abc"]),
            ("abc『abc『abc", ["abc", "『abc『abc"]),
            ("abc』abc』abc", ["abc』abc』", "abc"]),
            // '「''」''『''』'が混在するパターン
            ("abc「abc」abc『abc』abc", ["abc", "「abc」", "abc", "『abc』", "abc"]),
            ("abc『abc』abc「abc」abc", ["abc", "『abc』", "abc", "「abc」", "abc"]),
            ("「abc」abc『abc』abc", ["「abc」", "abc", "『abc』", "abc"]),
            ("『abc』abc「abc」abc", ["『abc』", "abc", "「abc」", "abc"]),
            ("abc「abc」『abc』abc", ["abc", "「abc」", "『abc』", "abc"]),
            ("abc『abc』「abc」abc", ["abc", "『abc』", "「abc」", "abc"]),
            ("abc「abc」abc『abc』", ["abc", "「abc」", "abc", "『abc』"]),
            ("abc『abc』abc「abc」", ["abc", "『abc』", "abc", "「abc」"]),
            ("abc「abc『abc』abc」abc", ["abc", "「abc『abc』abc」", "abc"]),
            ("abc『abc「abc」abc』abc", ["abc", "『abc「abc」abc』", "abc"]),
            ("abc「abc『abc』abc", ["abc", "「abc『abc』abc"]),
            ("abc『abc「abc」abc", ["abc", "『abc「abc」abc"]),
            ("abc「abc」abc』abc", ["abc「abc」abc』", "abc"]),
            ("abc『abc』abc」abc", ["abc『abc』abc」", "abc"]),
            ("abc「abc『abc", ["abc", "「abc『abc"]),
            ("abc『abc「abc", ["abc", "『abc「abc"]),
            ("abc」abc』abc", ["abc」abc』", "abc"]),
            ("abc』abc」abc", ["abc』abc」", "abc"]),
            ("abc』abc』abc", ["abc』abc』", "abc"])
        ];
        return cases.Select(c => new object[] { c.Item1, c.Item2 }).ToArray();
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void SplitBraceTest(string text, List<string> expected)
    {
        var service = new SplitBraceService();
        Assert.Equal(expected, service.SplitBrace(text));
    }
}
