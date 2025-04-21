import requests
import json

# API端点
BASE_URL = "http://localhost:8000"

def test_explain_schedule():
    """测试课表解释API"""
    print("===== 测试课表解释API =====")
    
    # 测试数据
    test_item = {
        "courseName": "计算机导论",
        "courseCode": "CS101",
        "teacherName": "张教授",
        "classroom": "主楼A-101",
        "dayName": "周一",
        "startTime": "08:00",
        "endTime": "10:00"
    }
    
    try:
        print("发送请求到课表解释API...")
        response = requests.post(
            f"{BASE_URL}/api/llm/explain-schedule",
            json={"scheduleItem": test_item}
        )
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            
            if "timeRationale" in result:
                print(f"\n时间原因: {result['timeRationale'][:150]}...")
                
            if "classroomRationale" in result:
                print(f"\n教室原因: {result['classroomRationale'][:150]}...")
                
            if "teacherRationale" in result:
                print(f"\n教师原因: {result['teacherRationale'][:150]}...")
                
            if "overallRationale" in result:
                print(f"\n整体原因: {result['overallRationale'][:150]}...")
                
            if "alternativesConsidered" in result:
                alternatives = result["alternativesConsidered"]
                print(f"\n考虑的替代方案 ({len(alternatives)}个):")
                for alt in alternatives:
                    print(f"  - 类型: {alt.get('type')}")
                    print(f"    替代选项: {alt.get('alternative')}")
                    print(f"    未选择原因: {alt.get('whyNotChosen')}")
            
            print("\n完整JSON响应:")
            print(json.dumps(result, indent=2, ensure_ascii=False))
        else:
            print(f"错误: {response.text}")
    
    except Exception as e:
        print(f"请求异常: {str(e)}")

if __name__ == "__main__":
    test_explain_schedule() 