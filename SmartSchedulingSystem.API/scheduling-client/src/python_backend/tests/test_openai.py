import openai
import os
from dotenv import load_dotenv

print("===== OpenAI API 测试脚本 =====")

# 加载环境变量
load_dotenv()

# 配置OpenAI API
api_key = os.getenv("OPENAI_API_KEY")
if not api_key:
    print("错误: 未找到OpenAI API密钥。请确保在.env文件中设置了OPENAI_API_KEY")
    exit(1)

openai.api_key = api_key
print(f"API密钥: {api_key[:5]}*********************")

try:
    # 尝试简单的API调用
    response = openai.chat.completions.create(
        model="gpt-3.5-turbo",
        messages=[
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": "Hello, how are you?"}
        ],
        temperature=0.7,
        max_tokens=100,
    )
    print("\n==== OpenAI API调用成功 ====")
    print(f"响应: {response.choices[0].message.content}")
    print("==========================\n")
    
    # 测试约束分析API的模拟调用
    print("\n==== 测试约束分析API调用 ====")
    constraint_prompt = """
    分析以下课程安排需求描述，提取所有明确或隐含的约束条件。
    
    需求描述：
    我们需要安排一个大型课程，需要容纳120名学生，教室需要有投影设备，张教授只能在周一和周三上午上课，每次课程时长2小时。
    
    请以JSON格式返回结果。
    """
    
    response = openai.chat.completions.create(
        model="gpt-3.5-turbo",
        messages=[
            {"role": "system", "content": "You are a scheduling system analysis expert. Your responses should be valid JSON objects only."},
            {"role": "user", "content": constraint_prompt}
        ],
        temperature=0.3,
        max_tokens=1000,
        response_format={"type": "json_object"}
    )
    
    print("约束分析API调用成功")
    print(f"响应: {response.choices[0].message.content}")
    print("==========================\n")
    
except Exception as e:
    print("\n==== OpenAI API调用失败 ====")
    print(f"错误: {str(e)}")
    print("==========================\n") 