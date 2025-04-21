import requests
import json

# API端点
BASE_URL = "http://localhost:8000"

def test_constraints():
    """测试约束分析API"""
    print("===== 测试约束分析API =====")
    
    test_inputs = [
        "我们需要安排一个大型课程，需要容纳120名学生，教室需要有投影设备，张教授只能在周一和周三上午上课，每次课程时长2小时。",
        "需要为30名学生安排一个小型研讨课，需要在下午进行，每周一次，总共持续8周。该课程需要使用实验设备。",
        "王教授的高级编程课需要在计算机实验室进行，学生需要使用电脑。课程时长为3小时，学生人数约为45人。"
    ]
    
    for i, input_text in enumerate(test_inputs):
        print(f"\n\n测试用例 {i+1}: {input_text[:60]}...")
        
        try:
            print("发送请求到约束分析API...")
            response = requests.post(
                f"{BASE_URL}/api/llm/analyze-constraints",
                json={"input": input_text}
            )
            
            print(f"状态码: {response.status_code}")
            
            if response.status_code == 200:
                result = response.json()
                print("\n响应结果:")
                
                if "explicitConstraints" in result:
                    explicit = result["explicitConstraints"]
                    print(f"\n显式约束 ({len(explicit)}个):")
                    for constraint in explicit:
                        print(f"  - {constraint.get('name')}: {constraint.get('description')}")
                        print(f"    类型: {constraint.get('type')}, 权重: {constraint.get('weight')}")
                
                if "implicitConstraints" in result:
                    implicit = result["implicitConstraints"]
                    print(f"\n隐式约束 ({len(implicit)}个):")
                    for constraint in implicit:
                        print(f"  - {constraint.get('name')}: {constraint.get('description')}")
                        print(f"    类型: {constraint.get('type')}, 权重: {constraint.get('weight')}")
                
                print("\n完整JSON响应:")
                print(json.dumps(result, indent=2, ensure_ascii=False))
            else:
                print(f"错误: {response.text}")
        
        except Exception as e:
            print(f"请求异常: {str(e)}")
            
if __name__ == "__main__":
    test_constraints() 