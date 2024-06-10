class TestClass():
    b_state=[]
    u_state=[]
    def __init__(self,adr):
        self.adr = adr
    def updateadr(self,state):
        TestClass.b_state=TestClass.u_state
        TestClass.u_state=state
        # print("adr",self.adr)
        print("b_state",self.b_state)
        print("u_state",self.u_state)
if __name__ == '__main__':
    adr=[2]
    astate=[3]
    bstate=[4]
    f=TestClass(adr)
    f.updateadr(astate)
    g=TestClass(adr)
    g.updateadr(bstate)