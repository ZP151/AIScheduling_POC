import requests
import json
import os
import sys

# API端点
BASE_URL = "http://localhost:8000"

def test_conflict_analysis():
    """测试冲突分析API"""
    print("===== 冲突分析API单独测试 =====")
    
    # 测试数据 - 教师冲突
    test_conflict_teacher = {
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
    
    # 测试数据 - 教室冲突
    test_conflict_classroom = {
        "description": "同一教室在同一时段被安排了两门不同的课程",
        "type": "Classroom Conflict",
        "involvedCourses": [
            {
                "name": "数据结构",
                "code": "CS201",
                "teacher": "李教授",
                "classroom": "主楼A-102",
                "timeSlot": "周二 10:00-11:30"
            },
            {
                "name": "高级算法",
                "code": "CS401",
                "teacher": "王教授",
                "classroom": "主楼A-102",
                "timeSlot": "周二 10:00-11:30"
            }
        ]
    }
    
    test_conflicts = [
        ("教师冲突", test_conflict_teacher),
        ("教室冲突", test_conflict_classroom)
    ]
    
    for conflict_type, conflict_data in test_conflicts:
        print(f"\n\n----- 测试 {conflict_type} -----")
        print(f"冲突描述: {conflict_data['description']}")
        print(f"冲突类型: {conflict_data['type']}")
        print(f"涉及课程数量: {len(conflict_data['involvedCourses'])}")
        
        try:
            print("\n发送请求到冲突分析API...")
            response = requests.post(
                f"{BASE_URL}/api/llm/analyze-conflicts",
                json={"conflict": conflict_data}
            )
            
            print(f"状态码: {response.status_code}")
            
            if response.status_code == 200:
                result = response.json()
                print("\n响应结果:")
                
                if "rootCause" in result:
                    print(f"\n根本原因: {result['rootCause']}")
                    
                if "solutions" in result:
                    solutions = result["solutions"]
                    print(f"\n解决方案 ({len(solutions)}个):")
                    for solution in solutions:
                        print(f"  - 方案 {solution.get('id')}: {solution.get('description')}")
                        print(f"    兼容性: {solution.get('compatibility')}%")
                        print(f"    影响:")
                        for impact in solution.get('impacts', []):
                            print(f"      * {impact}")
                
                print("\n完整JSON响应:")
                print(json.dumps(result, indent=2, ensure_ascii=False))
            else:
                print(f"错误响应: {response.text}")
        
        except Exception as e:
            print(f"请求异常: {str(e)}")
    
    print("\n===== 冲突分析API测试完成 =====")

if __name__ == "__main__":
    test_conflict_analysis() 