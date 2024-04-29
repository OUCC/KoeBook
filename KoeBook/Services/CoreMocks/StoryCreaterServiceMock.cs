using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Services.CoreMocks
{
    public class StoryCreaterServiceMock : IStoryCreaterService
    {
        public ValueTask<string> CreateStoryAsync(StoryGenre genre, string instruction, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("""
            <?xml version="1.0" encoding="UTF-8"?>
            <Book>
                <Title>境界線の向こう側</Title>
                <Content>
                    <Section>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                    </Section>
                    <Section>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>はんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで</Text><Ruby><Rb>凛</Rb><Rt>りん</Rt></Ruby><Text>とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活</Text><Ruby><Rb>仲間</Rb><Rt>なかま</Rt></Ruby><Text>たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                    </Section>
                </Content>
            </Book>
            """);
        }
    }
}
