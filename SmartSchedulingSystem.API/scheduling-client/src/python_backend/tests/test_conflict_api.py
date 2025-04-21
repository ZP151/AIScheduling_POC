import requests
import json
import sys

# API端点
BASE_URL = "http://localhost:8000"

def test_conflict_api():
    """测试冲突分析API"""
    print("开始测试冲突分析API...")
    
    # 测试数据 - 教师冲突
    test_conflict = {
        "description": "教师在相同时段被安排了两门不同的课程",
        "type": "Teacher Conflict",
        "involvedCourses": [
            {
                "name": "计算机导论",
                "code": "CS101",
                "teacher": "张教授",
                "classroom": "主楼A-101",
                "timeSlot": "周一 08:00-09:30"
            },
            {
                "name": "算法设计",
                "code": "CS301",
                "teacher": "张教授",
                "classroom": "主楼B-203",
                "timeSlot": "周一 08:00-09:30"
            }
        ]
    }
    
    print(f"测试数据: {json.dumps(test_conflict, ensure_ascii=False, indent=2)}")
    
    try:
        print("\n发送请求到冲突分析API...")
        # 直接调用API
        response = requests.post(
            f"{BASE_URL}/api/llm/analyze-conflicts",
            json={"conflict": test_conflict}
        )
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            print(json.dumps(result, ensure_ascii=False, indent=2))
            print("\n测试成功完成!")
        else:
            print(f"错误响应: {response.text}")
    except Exception as e:
        print(f"请求异常: {str(e)}")
        print("可能的原因:")
        print("1. API服务未运行")
        print("2. 网络连接问题")
        print("3. API路径错误")
        print("\n请确保API服务已启动，并且运行在正确的端口上")

if __name__ == "__main__":
    test_conflict_api() 