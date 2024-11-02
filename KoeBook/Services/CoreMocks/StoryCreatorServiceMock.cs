using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Services.CoreMocks
{
    public class StoryCreatorServiceMock : IStoryCreatorService
    {
        public ValueTask<string> CreateStoryAsync(StoryGenre genre, string instruction, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("""
            <?xml version="1.0" encoding="UTF-8"?>
            <Book>
                <Title>キャンパスの春風</Title>
                <Content>
                    <Section>
                        <Paragraph><Text>東京の名門私立大学に入学した佐藤健太は、高校時代から</Text><Ruby><Rb>憧</Rb><Rt>あこが</Rt></Ruby><Text>れていた大学生活に胸を</Text><Ruby><Rb>躍</Rb><Rt>おど</Rt></Ruby><Text>らせていた。オリエンテーションの日、キャンパスを歩いていると、</Text><Ruby><Rb>鮮</Rb><Rt>あざ</Rt></Ruby><Ruby><Rb>烈</Rb><Rt>や</Rt></Ruby><Text>かなピンク色の花が目に入った。</Text></Paragraph>

                        <Paragraph><Text>「あれは河津桜だよ」</Text></Paragraph>

                        <Paragraph><Text>隣から聞こえてきた澄んだ声に振り向くと、健太の視線の先には、凛とした佇まいの女性がいた。黒髪をさらりとまとめ、大きな瞳が印象的な彼女は、健太の高校の後輩で、今年この大学に入学した山田美咲だった。</Text></Paragraph>

                        <Paragraph><Text>「美咲じゃないか！　よく気づいたね」</Text></Paragraph>

                        <Paragraph><Text>「先輩こそ。私も東京の大学に来るなんて思ってもみませんでした」</Text></Paragraph>

                        <Paragraph><Text>高校時代、美咲は目立たない存在だったが、その聡明さと優しさで健太の目に留まっていた。そんな彼女が同じキャンパスにいることに、健太は喜びを隠せなかった。</Text></Paragraph>

                        <Paragraph><Text>「大学では何かサークルに入るつもり？」</Text></Paragraph>

                        <Paragraph><Text>「写真部に興味があるんです。高校の頃からカメラが好きで」</Text></Paragraph>

                        <Paragraph><Text>「そうなんだ。じゃあ一緒に写真部に入ろうよ。きっと楽しいよ」</Text></Paragraph>

                        <Paragraph><Text>「ええ、ぜひ！」</Text></Paragraph>

                        <Paragraph><Text>美咲の目を見つめながら会話をする健太の胸中に、新しい出会いへの期待が芽生えていた。</Text></Paragraph>
                    </Section>

                    <Section>
                        <Paragraph><Text>春のキャンパスは鮮やかな新緑に包まれ、至る所で学生たちの弾む声が聞こえる。健太と美咲は写真部の新入部員として、思い思いの構図でシャッターを切っていた。</Text></Paragraph>

                        <Paragraph><Text>「ねえ先輩、ここからの眺めって素敵だと思いません？」</Text></Paragraph>

                        <Paragraph><Text>美咲に呼ばれ駆け寄ると、そこには</Text><Ruby><Rb>木漏</Rb><Rt>こも</Rt></Ruby><Ruby><Rb>日</Rb><Rt>れび</Rt></Ruby><Text>の中で輝く新緑が広がっていた。一瞬、言葉を失った健太だったが、美咲の感性の鋭さに感心し、カメラを構えた。</Text></Paragraph>

                        <Paragraph><Text>「うん、本当に綺麗だ。美咲の目に狙いがあるね」</Text></Paragraph>

                        <Paragraph><Text>「そんな、先輩だってすごく上手いじゃないですか」</Text></Paragraph>

                        <Paragraph><Text>そう言って美咲が見せた笑顔に、健太の心はときめいた。いつの間にか、彼女の存在が健太にとってかけがえのないものになっていた。</Text></Paragraph>
                    </Section>

                    <Section>
                        <Paragraph><Text>「健太君、ちょっといい？」</Text></Paragraph>

                        <Paragraph><Text>ある日の放課後、美咲が真剣な面持ちで健太を呼び止めた。</Text></Paragraph>

                        <Paragraph><Text>「どうしたの、美咲」</Text></Paragraph>

                        <Paragraph><Text>「私、健太君のこと、ずっと前から好きでした！　付き合ってください！」</Text></Paragraph>

                        <Paragraph><Text>唐突の告白に健太は心臓が止まる思いがした。いつの間にか特別な存在になっていた美咲。その瞳を見つめ、健太は言葉を紡いだ。</Text></Paragraph>

                        <Paragraph><Text>「美咲、俺も同じ気持ちだよ。よろしくね」</Text></Paragraph>

                        <Paragraph><Text>春風が二人の頬を優しく撫でる。キャンパスに響く若者たちの歓声が、新たな恋の始まりを祝福しているようだった。</Text></Paragraph>
                    </Section>
                </Content>
            </Book>
            """);
        }
    }
}
