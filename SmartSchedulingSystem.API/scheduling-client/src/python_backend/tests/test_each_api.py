"""
分步测试每个API的脚本
"""
import requests
import json
import time

BASE_URL = "http://localhost:8080"

def print_header(title):
    print("\n" + "=" * 60)
    print(f"{title}")
    print("=" * 60)

def test_chat_api():
    """测试聊天API"""
    print_header("1. 测试聊天API")
    
    url = f"{BASE_URL}/api/llm/chat"
    data = {
        "message": "你好，请告诉我关于智能排课系统的功能",
        "conversation": []
    }
    
    try:
        print(f"请求地址: {url}")
        print(f"请求数据: {json.dumps(data, ensure_ascii=False)}")
        
        response = requests.post(url, json=data)
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            print(json.dumps(result, ensure_ascii=False, indent=2))
            return True
        else:
            print(f"请求失败: {response.text}")
            return False
    except Exception as e:
        print(f"发生错误: {str(e)}")
        return False

def test_constraint_api():
    """测试约束分析API"""
    print_header("2. 测试约束分析API")
    
    url = f"{BASE_URL}/api/llm/analyze-constraints"
    data = {
        "input": "我们需要安排一个大型课程，需要容纳120名学生，教室需要有投影设备，张教授只能在周一和周三上午上课，每次课程时长2小时。"
    }
    
    try:
        print(f"请求地址: {url}")
        print(f"请求数据: {json.dumps(data, ensure_ascii=False)}")
        
        response = requests.post(url, json=data)
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            print(f"显式约束数量: {len(result.get('explicitConstraints', []))}")
            print(f"隐式约束数量: {len(result.get('implicitConstraints', []))}")
            
            print("\n显式约束:")
            for constraint in result.get('explicitConstraints', [])[:2]:  # 只显示前2个
                print(f"- {constraint.get('name')}: {constraint.get('description')}")
            
            return True
        else:
            print(f"请求失败: {response.text}")
            return False
    except Exception as e:
        print(f"发生错误: {str(e)}")
        return False

def test_conflict_api():
    """测试冲突分析API"""
    print_header("3. 测试冲突分析API")
    
    url = f"{BASE_URL}/api/llm/analyze-conflicts"
    data = {
        "conflict": {
            "description": "两门课程在同一时间使用同一教室",
            "type": "教室冲突",
            "involvedCourses": [
                {
                    "name": "高等数学",
                    "code": "MATH101",
                    "teacher": "张教授",
                    "classroom": "科学楼101",
                    "timeSlot": "周一 9:00-11:00"
                },
                {
                    "name": "物理学导论",
                    "code": "PHYS101",
                    "teacher": "李教授",
                    "classroom": "科学楼101",
                    "timeSlot": "周一 9:00-11:00"
                }
            ]
        }
    }
    
    try:
        print(f"请求地址: {url}")
        print(f"请求数据: {json.dumps(data, ensure_ascii=False, indent=2)}")
        
        response = requests.post(url, json=data)
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            print(f"根本原因: {result.get('rootCause', '未提供')[:100]}...")
            
            print("\n解决方案数量:", len(result.get('solutions', [])))
            if result.get('solutions'):
                solution = result.get('solutions')[0]
                print(f"第一个方案: {solution.get('description', '未提供描述')}")
                print(f"兼容性评分: {solution.get('compatibility', 0)}")
            
            return True
        else:
            print(f"请求失败: {response.text}")
            return False
    except Exception as e:
        print(f"发生错误: {str(e)}")
        return False

def test_explain_api():
    """测试日程解释API"""
    print_header("4. 测试日程解释API")
    
    url = f"{BASE_URL}/api/llm/explain-schedule"
    data = {
        "scheduleItem": {
            "courseName": "高等数学",
            "courseCode": "MATH101",
            "teacherName": "张教授",
            "classroom": "科学楼101",
            "dayName": "周一",
            "startTime": "9:00",
            "endTime": "11:00"
        }
    }
    
    try:
        print(f"请求地址: {url}")
        print(f"请求数据: {json.dumps(data, ensure_ascii=False, indent=2)}")
        
        response = requests.post(url, json=data)
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            
            if "timeRationale" in result:
                print(f"时间原因: {result.get('timeRationale', '未提供')[:80]}...")
            
            if "classroomRationale" in result:
                print(f"教室原因: {result.get('classroomRationale', '未提供')[:80]}...")
            
            if "alternativesConsidered" in result:
                print(f"替代方案数量: {len(result.get('alternativesConsidered', []))}")
            
            return True
        else:
            print(f"请求失败: {response.text}")
            return False
    except Exception as e:
        print(f"发生错误: {str(e)}")
        return False

def test_optimize_api():
    """测试参数优化API"""
    print_header("5. 测试参数优化API")
    
    url = f"{BASE_URL}/api/llm/optimize-parameters"
    data = {
        "currentParameters": {
            "教师工作负荷平衡权重": 0.7,
            "学生课表紧凑度权重": 0.5,
            "教室类型匹配权重": 0.8
        },
        "historicalData": {
            "previousSchedules": [
                {
                    "semester": "2023春季",
                    "statistics": {
                        "冲突数": 5,
                        "平均教师满意度": 0.75,
                        "平均学生满意度": 0.65
                    }
                }
            ]
        }
    }
    
    try:
        print(f"请求地址: {url}")
        print(f"请求数据: {json.dumps(data, ensure_ascii=False, indent=2)}")
        
        response = requests.post(url, json=data)
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print("\n响应结果:")
            
            print(f"优化建议数量: {len(result.get('optimizationSuggestions', []))}")
            print(f"新参数建议数量: {len(result.get('newParameterSuggestions', []))}")
            
            if result.get('optimizationSuggestions'):
                suggestion = result.get('optimizationSuggestions')[0]
                print(f"\n第一个优化建议:")
                print(f"参数名: {suggestion.get('parameterName', '未提供')}")
                print(f"当前值: {suggestion.get('currentValue', '未提供')}")
                print(f"建议值: {suggestion.get('suggestedValue', '未提供')}")
            
            return True
        else:
            print(f"请求失败: {response.text}")
            return False
    except Exception as e:
        print(f"发生错误: {str(e)}")
        return False

def main():
    print("开始测试各个API...")
    
    results = {
        "聊天API": test_chat_api(),
        "约束分析API": test_constraint_api(),
        "冲突分析API": test_conflict_api(),
        "日程解释API": test_explain_api(),
        "参数优化API": test_optimize_api()
    }
    
    print("\n" + "=" * 60)
    print("测试结果汇总:")
    print("=" * 60)
    
    for api_name, success in results.items():
        result = "✅ 成功" if success else "❌ 失败"
        print(f"{api_name}: {result}")

if __name__ == "__main__":
    main() 