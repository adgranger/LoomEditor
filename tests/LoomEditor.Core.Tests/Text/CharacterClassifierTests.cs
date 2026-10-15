// Ported from microsoft/vscode: src/vs/editor/test/common/core/characterClassifier.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class CharacterClassifierTests
{
    // Monaco: 'works'
    [Fact]
    public void Works_Monaco()
    {
        var classifier = new CharacterClassifier<int>(0);

        Assert.Equal(0, classifier.Get(-1));
        Assert.Equal(0, classifier.Get(0));
        Assert.Equal(0, classifier.Get('a'));
        Assert.Equal(0, classifier.Get('b'));
        Assert.Equal(0, classifier.Get('z'));
        Assert.Equal(0, classifier.Get(255));
        Assert.Equal(0, classifier.Get(1000));
        Assert.Equal(0, classifier.Get(2000));

        classifier.Set('a', 1);
        classifier.Set('z', 2);
        classifier.Set(1000, 3);

        Assert.Equal(0, classifier.Get(-1));
        Assert.Equal(0, classifier.Get(0));
        Assert.Equal(1, classifier.Get('a'));
        Assert.Equal(0, classifier.Get('b'));
        Assert.Equal(2, classifier.Get('z'));
        Assert.Equal(0, classifier.Get(255));
        Assert.Equal(3, classifier.Get(1000));
        Assert.Equal(0, classifier.Get(2000));
    }

    [Fact]
    public void CharacterSet_AddHasClear_Works()
    {
        var set = new CharacterSet();
        set.Add('a');
        set.Add(1000);

        Assert.True(set.Has('a'));
        Assert.True(set.Has(1000));
        Assert.False(set.Has('b'));

        set.Clear();
        Assert.False(set.Has('a'));
        Assert.False(set.Has(1000));
    }
}
