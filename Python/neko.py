# クラスの定義
class Neko:
  def __init__(self, breed, color, tail, neko_skill):
    self.breed = breed        # 猫種
    self.color = color # 毛の色
    self.tail = tail   # しっぽの有無
    self.neko_skill = neko_skill # ねこ技を設定

  # 属性に対して操作を行うメソッドの定義
  def hissatsu(self):
    print(f'{self.breed}の必殺技{self.neko_skill}!')
class Koneko(Neko):
    def __init__(self, color, tail, name, neko_skill):
        super().__init__('マンチカン', color, tail, neko_skill)
        print(self.breed)
        self.name = name

    # hissatsuメソッドをオーバーライド
    def hissatsu(self):
        print(f'{self.color}色の赤ちゃんマンチカンの必殺技{self.neko_skill}!')

    # 新しいメソッド（飼い主にごあいさつ）の追加
    def say_hello(self):
        print(f'はじめまして。ワタシの名前は{self.name}だにゃ。')

if __name__ == '__main__':
    neko1 = Neko('マンチカン', '真っ白', '長い', 'ネコパンチ')
    koneko = Koneko('オフホワイト', '短い', 'しろねこ', 'ねこじゃらし攻撃')
    koneko.hissatsu()
    neko2 = Neko('スコティッシュフォールド', '三毛猫', 'ふさふさ', 'ネコキック')
    koneko = Koneko('オフホワイト', '短い', 'しろねこ', 'ねこじゃらし攻撃')
    koneko.hissatsu()