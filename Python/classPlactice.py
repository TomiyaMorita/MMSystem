class TestClass():
    in_bstate={}
    in_ustate={}
    ex_bstate={}
    ex_ustate={}
    awaitingupdate = {}
    icelimitcount = 0
    def __init__(self,in_ustate,ex_ustate):
        TestClass.in_ustate=in_ustate
        TestClass.ex_ustate = ex_ustate
    @classmethod
    def updateState(cls, inState={},exState={}):
        #更新前のステータスbeforedictに保存
        cls.in_bstate = cls.in_ustate.copy()
        #全てのステータスをalldictに統合
        cls.in_ustate.update(inState)
        cls.ex_bstate = cls.ex_ustate.copy()
        cls.ex_ustate.update(exState)
        print("updateStatein_bstate",cls.in_bstate)
        print("updateStatein_ustate",cls.in_ustate)
        print("updateStateex_bstate",cls.ex_bstate)
        print("updateStateex_ustate",cls.ex_ustate)
        b={}
        a,b=cls.plcUpdated(cls)
        cls.in_ustate.update(b)
      
        return a,b
    def plcUpdated(self):
        self.in_ustate.update(a=100)
        a=[1]
        # print("plcUpdatedin_bstate",self.in_bstate)
        print("plcUpdatedin_ustate",self.in_ustate)
        # print("plcUpdatedex_bstate",self.ex_bstate)
        # print("plcUpdatedex_ustate",self.ex_ustate)
        return a,self.in_ustate

def updatestate(a,b):
    a,b=TestClass.updateState(a)
    print(a)
if __name__ == "__main__":
    a={
        "a":1
    }
    b={
        "b":2
    }
    a2={
        "a":3
    }
    b2={
        "b":4
    }
    a3={
        "a":5
    }
    b3={
        "b":6
    }
    s=TestClass(a,b)
    # s.updateState(a2,b2)
    # print("print",TestClass.in_ustate)
    # updatestate(a3,b3)
    # print("print",TestClass.in_ustate)
    # updatestate(a,b)
    # print("print",TestClass.in_ustate)